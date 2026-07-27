using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace JTest.AcceptanceTests.TestSupport;

/// <summary>
/// A tiny in-process HTTP API the acceptance suites run against: login,
/// order lifecycle with polling, stock lookups, and a header echo used to
/// prove redaction end to end.
/// </summary>
internal sealed class LocalEchoApi : IAsyncDisposable
{
    private readonly HttpListener listener;
    private readonly CancellationTokenSource stopping = new();
    private readonly Task pump;
    private int orderPolls;

    internal LocalEchoApi()
    {
        var port = FindFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        listener = new HttpListener();
        listener.Prefixes.Add($"{BaseUrl}/");
        listener.Start();
        pump = Task.Run(PumpAsync);
    }

    internal string BaseUrl { get; }

    public async ValueTask DisposeAsync()
    {
        await stopping.CancelAsync();
        listener.Stop();
        try
        {
            await pump;
        }
        catch (HttpListenerException)
        {
        }
        catch (ObjectDisposedException)
        {
        }

        stopping.Dispose();
    }

    private async Task PumpAsync()
    {
        while (!stopping.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (Exception) when (stopping.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await HandleAsync(context);
            }
            catch (Exception)
            {
                context.Response.StatusCode = 500;
            }
            finally
            {
                context.Response.Close();
            }
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        var path = context.Request.Url!.AbsolutePath;
        var method = context.Request.HttpMethod;

        JsonObject body;
        switch (method, path)
        {
            case ("POST", "/auth/login"):
                using (var reader = new StreamReader(context.Request.InputStream))
                {
                    var request = JsonNode.Parse(await reader.ReadToEndAsync())!.AsObject();
                    body = new JsonObject
                    {
                        ["token"] = $"tok-{request["user"]!.GetValue<string>()}",
                        ["expiresIn"] = 3600,
                    };
                }

                break;

            case ("POST", "/orders"):
                orderPolls = 0;
                context.Response.StatusCode = 201;
                body = new JsonObject { ["id"] = "ord-1", ["state"] = "pending" };
                break;

            case ("GET", "/orders/ord-1"):
                orderPolls++;
                body = new JsonObject { ["id"] = "ord-1", ["state"] = orderPolls >= 3 ? "ready" : "pending" };
                break;

            case ("GET", "/echo-auth"):
                body = new JsonObject
                {
                    ["authorization"] = context.Request.Headers["Authorization"] ?? string.Empty,
                };
                break;

            case ("GET", _) when path.StartsWith("/stock/", StringComparison.Ordinal):
                body = new JsonObject { ["sku"] = path["/stock/".Length..], ["available"] = 42 };
                break;

            default:
                context.Response.StatusCode = 404;
                body = new JsonObject { ["error"] = "not found" };
                break;
        }

        var bytes = Encoding.UTF8.GetBytes(body.ToJsonString());
        context.Response.ContentType = "application/json";
        await context.Response.OutputStream.WriteAsync(bytes);
    }

    private static int FindFreePort()
    {
        using var socket = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        var port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return port;
    }
}
