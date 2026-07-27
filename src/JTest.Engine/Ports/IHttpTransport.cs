namespace JTest.Engine.Ports;

/// <summary>HTTP transport used by http steps.</summary>
public interface IHttpTransport
{
    /// <summary>Sends one request and returns the raw response.</summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancels the exchange.</param>
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}
