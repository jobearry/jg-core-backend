using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Portfolio.Api.Tests.Fakes;

/// <summary>
/// Returns queued HTTP responses and records each request. The vendor tests use it
/// to pin request URLs, headers and retry behaviour without a network call.
/// </summary>
public sealed class QueuedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> _responses;

    public QueuedHttpMessageHandler(params (HttpStatusCode Status, string Body)[] responses)
    {
        _responses = new Queue<(HttpStatusCode, string)>(responses);
    }

    public List<Uri> RequestUris { get; } = new();

    public List<AuthenticationHeaderValue?> AuthorizationHeaders { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        AuthorizationHeaders.Add(request.Headers.Authorization);

        var (status, body) = _responses.Dequeue();

        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}
