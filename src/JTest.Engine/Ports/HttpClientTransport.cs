namespace JTest.Engine.Ports;

/// <summary>The real transport over one injected <see cref="HttpClient"/>.</summary>
public sealed class HttpClientTransport : IHttpTransport
{
    private readonly HttpClient client;

    /// <summary>Creates the transport.</summary>
    /// <param name="client">The client to send through; owned by the caller.</param>
    public HttpClientTransport(HttpClient client)
    {
        this.client = client;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
}
