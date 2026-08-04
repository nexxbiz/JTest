using System;
using System.Net;
using System.Text;
using JTest.Core.Execution;
using JTest.Core.Models;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
using JTest.Core.Templates;
using NSubstitute;
using Spectre.Console;
using Xunit;

namespace JTest.UnitTests.Http;

public class HttpCookieSessionTests
{
    /// <summary>Records the Cookie header of each request and sets a cookie on the first response.</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string?> CookieHeaders { get; } = new();
        private int _call;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CookieHeaders.Add(request.Headers.TryGetValues("Cookie", out var v) ? string.Join("; ", v) : null);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            if (_call == 0)
                response.Headers.TryAddWithoutValidation("Set-Cookie", "session=abc123; path=/");

            _call++;
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task Cookie_FromLogin_IsCarriedToLaterStep_InSameCase()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler);
        var context = new TestExecutionContext();

        await new HttpStep(client, new HttpStepConfiguration("POST", "https://api.test/login")).ExecuteAsync(context);
        await new HttpStep(client, new HttpStepConfiguration("GET", "https://api.test/data")).ExecuteAsync(context);

        Assert.Null(handler.CookieHeaders[0]);                        // login carried no cookie
        Assert.Contains("session=abc123", handler.CookieHeaders[1]);  // later step carried the session automatically
    }

    [Fact]
    public async Task Cookies_AreIsolatedBetweenCases()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler);
        var caseA = new TestExecutionContext();
        var caseB = new TestExecutionContext();

        await new HttpStep(client, new HttpStepConfiguration("POST", "https://api.test/login")).ExecuteAsync(caseA);
        await new HttpStep(client, new HttpStepConfiguration("GET", "https://api.test/data")).ExecuteAsync(caseB);

        Assert.Null(handler.CookieHeaders[1]); // caseB never saw caseA's cookie
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            response.Headers.TryAddWithoutValidation("Set-Cookie", "a=1");
            response.Headers.TryAddWithoutValidation("Set-Cookie", "b=2");
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task ResponseContract_HasKeyedHeaders_And_StatusCodeAlias()
    {
        var client = new HttpClient(new StubHandler());
        var context = new TestExecutionContext();

        var result = await new HttpStep(client, new HttpStepConfiguration("GET", "https://api.test/x")).ExecuteAsync(context);
        var data = (Dictionary<string, object?>)result.Data!;

        Assert.Equal(200, data["statusCode"]);
        Assert.Equal(200, data["status"]);

        var headers = (Dictionary<string, object?>)data["headers"]!;
        Assert.Contains("application/json", headers["content-type"]!.ToString()); // "Content-Type" read case-insensitively
        var setCookie = Assert.IsType<string[]>(headers["set-cookie"]);           // multi-valued → array
        Assert.Equal(2, setCookie.Length);
    }

    /// <summary>
    /// Regression: a login performed inside a <c>use</c> template must establish the enclosing case's
    /// cookie session, so a later step in the same case is authenticated. Previously the template's
    /// isolated scope created a throwaway cookie jar, dropping the login's Set-Cookie and 401-ing every
    /// subsequent step (FR-038/FR-039, US7, SC-013).
    /// </summary>
    [Fact]
    public async Task Cookie_FromLoginInsideUseTemplate_IsCarriedToLaterStep_InSameCase()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler);
        var caseContext = new TestExecutionContext();

        // A template whose single step logs in and receives a Set-Cookie.
        var template = new Template
        {
            Name = "login",
            Steps = new IStep[]
            {
                new HttpStep(client, new HttpStepConfiguration("POST", "https://api.test/login"))
            }
        };
        var templateContext = Substitute.For<ITemplateContext>();
        templateContext.GetTemplate("login").Returns(template);

        var useStep = new UseStep(
            Substitute.For<IAnsiConsole>(),
            templateContext,
            StepProcessor.Default,
            Substitute.For<IServiceProvider>(),
            new UseStepConfiguration(Template: "login", With: null));

        // Login happens INSIDE the `use` template...
        await useStep.ExecuteAsync(caseContext);
        // ...then a later step runs directly in the same case.
        await new HttpStep(client, new HttpStepConfiguration("GET", "https://api.test/data")).ExecuteAsync(caseContext);

        Assert.Null(handler.CookieHeaders[0]);                        // login (inside the template) carried no cookie
        Assert.Contains("session=abc123", handler.CookieHeaders[1]);  // the case step carried the template-established session
    }

    /// <summary>
    /// The cookie jar is shared across the <c>use</c> template boundary, but variables are NOT: a
    /// variable created inside the template does not leak into the case scope, and the case's own
    /// variables are untouched.
    /// </summary>
    [Fact]
    public async Task Variables_AreIsolated_AcrossUseTemplateBoundary_WhileCookiesAreShared()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler);
        var caseContext = new TestExecutionContext();
        caseContext.Variables["caseOnly"] = "parent-value";

        var template = new Template
        {
            Name = "login",
            Steps = new IStep[]
            {
                new HttpStep(client, new HttpStepConfiguration("POST", "https://api.test/login", Id: "loginResult"))
            }
        };
        var templateContext = Substitute.For<ITemplateContext>();
        templateContext.GetTemplate("login").Returns(template);

        var useStep = new UseStep(
            Substitute.For<IAnsiConsole>(),
            templateContext,
            StepProcessor.Default,
            Substitute.For<IServiceProvider>(),
            new UseStepConfiguration(Template: "login", With: null));

        await useStep.ExecuteAsync(caseContext);

        // Variables set inside the template do not leak into the case scope...
        Assert.False(caseContext.Variables.ContainsKey("loginResult"));
        Assert.False(caseContext.Variables.ContainsKey("this"));
        // ...and the case's own variables are untouched.
        Assert.Equal("parent-value", caseContext.Variables["caseOnly"]);
        // But the session cookie set inside the template is visible on the shared case jar.
        Assert.Contains("session=abc123", caseContext.Cookies.GetCookieHeader(new Uri("https://api.test/data")));
    }
}
