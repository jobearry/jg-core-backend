using System.Net;
using System.Text;
using JDB_Portfolio_API.ExternalService.Notion.Services;

namespace JDB_Portfolio_API.Api.Tests;

/// <summary>
/// Plan 4.6. The client no longer owns its base address, headers or timeout — those are
/// configured by the composition root — so these tests pin the two behaviours that are
/// still its responsibility: the path it requests, and the single retry.
/// </summary>
public class NotionClientServiceTests
{
    private const string MarkdownResponse =
        """{"object":"page","id":"page-id","markdown":"<page url=\"https://notion.so/page\"><mention-date start=\"2026-06-27\"/>About me</page>","truncated":false,"unknownBlockIds":[],"requestId":"req"}""";

    [Fact]
    public async Task QueryPageAsync_RequestsThePathRelativeToTheBaseAddress()
    {
        var handler = new SequencedHandler((HttpStatusCode.OK, MarkdownResponse));

        await CreateClient(handler).QueryPageAsync("page-id", CancellationToken.None);

        Assert.Equal(
            new Uri("https://api.notion.com/v1/pages/page-id/markdown"),
            Assert.Single(handler.RequestUris));
    }

    [Fact]
    public async Task QueryPageAsync_RetriesOnceAfterATransientFailure()
    {
        var handler = new SequencedHandler(
            (HttpStatusCode.InternalServerError, "{}"),
            (HttpStatusCode.OK, MarkdownResponse));

        var pages = await CreateClient(handler).QueryPageAsync("page-id", CancellationToken.None);

        Assert.Single(pages);
        Assert.Equal(2, handler.RequestUris.Count);
    }

    [Fact]
    public async Task QueryPageAsync_GivesUpAfterOneRetry()
    {
        var handler = new SequencedHandler(
            (HttpStatusCode.ServiceUnavailable, "{}"),
            (HttpStatusCode.TooManyRequests, "{}"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateClient(handler).QueryPageAsync("page-id", CancellationToken.None));

        Assert.Equal(2, handler.RequestUris.Count);
    }

    [Fact]
    public async Task QueryPageAsync_DoesNotRetryARequestTheServerRejected()
    {
        var handler = new SequencedHandler((HttpStatusCode.NotFound, "{}"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateClient(handler).QueryPageAsync("page-id", CancellationToken.None));

        Assert.Single(handler.RequestUris);
    }

    private static NotionClientService CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.notion.com") });

    private sealed class SequencedHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _responses;

        public SequencedHandler(params (HttpStatusCode Status, string Body)[] responses)
        {
            _responses = new Queue<(HttpStatusCode, string)>(responses);
        }

        public List<Uri> RequestUris { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);

            var (status, body) = _responses.Dequeue();

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
