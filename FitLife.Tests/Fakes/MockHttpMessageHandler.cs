using System.Net;
using System.Text;
using System.Text.Json;

namespace FitLife.Tests.Fakes;

/// <summary>
/// Configurable HttpMessageHandler that returns a new response per call using a factory,
/// so the same handler can be used with services that call the same endpoint multiple times
/// (e.g. AuthenticationService.LoginAsync, which sends the request twice to work around
/// a stream-consumption issue in the production code).
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        => _responseFactory = responseFactory;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_responseFactory(request));

    // Convenience: always returns the same JSON-serialised body with a 200 OK.
    public static HttpClient CreateJsonClient<T>(T body, HttpStatusCode status = HttpStatusCode.OK,
        string baseAddress = "http://localhost/")
    {
        var json = JsonSerializer.Serialize(body);
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
    }

    // Convenience: returns a non-success status code with an empty body.
    public static HttpClient CreateErrorClient(HttpStatusCode status = HttpStatusCode.InternalServerError,
        string baseAddress = "http://localhost/")
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });
        return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
    }

    // Convenience: always throws HttpRequestException to simulate a network failure.
    public static HttpClient CreateNetworkErrorClient(string baseAddress = "http://localhost/")
    {
        var handler = new ThrowingHandler();
        return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Simulated network failure");
    }
}
