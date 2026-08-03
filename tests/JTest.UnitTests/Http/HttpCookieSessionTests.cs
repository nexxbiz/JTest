using System.Net;
using System.Text;
using JTest.Core.Execution;
using JTest.Core.Steps;
using JTest.Core.Steps.Configuration;
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
}
