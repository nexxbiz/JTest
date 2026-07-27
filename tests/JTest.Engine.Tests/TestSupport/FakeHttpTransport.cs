using System.Net;
using System.Text;
using JTest.Engine.Ports;

namespace JTest.Engine.Tests.TestSupport;

/// <summary>
/// Scripted HTTP transport: responses are dequeued per request; a handler
/// delegate can inspect the request or fail deliberately.
/// </summary>
internal sealed class FakeHttpTransport : IHttpTransport
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> handlers = new();

    internal List<(string Method, string Url, string? AuthorizationHeader)> Requests { get; } = [];

    internal FakeHttpTransport EnqueueJson(int status, string json)
    {
        handlers.Enqueue((_, _) => Task.FromResult(JsonResponse(status, json)));
        return this;
    }

    internal FakeHttpTransport EnqueueThrow(Exception exception)
    {
        handlers.Enqueue((_, _) => Task.FromException<HttpResponseMessage>(exception));
        return this;
    }

    internal FakeHttpTransport EnqueueHang()
    {
        handlers.Enqueue(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new InvalidOperationException("unreachable");
        });
        return this;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((
            request.Method.Method,
            request.RequestUri!.ToString(),
            request.Headers.TryGetValues("Authorization", out var values) ? string.Join(",", values) : null));

        if (handlers.Count == 0)
        {
            return JsonResponse(200, "{}");
        }

        return await handlers.Dequeue()(request, cancellationToken);
    }

    private static HttpResponseMessage JsonResponse(int status, string json) =>
        new((HttpStatusCode)status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
}
