using System.Net;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Features.Notion;

namespace Portfolio.Api.Tests;

/// <summary>
/// Plan 4.6. The client no longer owns its base address, headers or timeout — those are
/// configured by the composition root — so these tests pin the two behaviours that are
/// still its responsibility: the path it requests, and the single retry.
/// </summary>
public class NotionClientServiceTests
{
    [Fact]
    public async Task QueryPageAsync_RequestsThePathRelativeToTheBaseAddress()
    {
        var handler = new QueuedHttpMessageHandler((HttpStatusCode.OK, NotionTestPayloads.MarkdownResponse));

        await CreateClient(handler).QueryPageAsync("page-id", CancellationToken.None);

        Assert.Equal(
            new Uri("https://api.notion.com/v1/pages/page-id/markdown"),
            Assert.Single(handler.RequestUris));
    }

    [Fact]
    public async Task QueryPageAsync_RetriesOnceAfterATransientFailure()
    {
        var handler = new QueuedHttpMessageHandler(
            (HttpStatusCode.InternalServerError, "{}"),
            (HttpStatusCode.OK, NotionTestPayloads.MarkdownResponse));

        var pages = await CreateClient(handler).QueryPageAsync("page-id", CancellationToken.None);

        Assert.Single(pages);
        Assert.Equal(2, handler.RequestUris.Count);
    }

    [Fact]
    public async Task QueryPageAsync_GivesUpAfterOneRetry()
    {
        var handler = new QueuedHttpMessageHandler(
            (HttpStatusCode.ServiceUnavailable, "{}"),
            (HttpStatusCode.TooManyRequests, "{}"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateClient(handler).QueryPageAsync("page-id", CancellationToken.None));

        Assert.Equal(2, handler.RequestUris.Count);
    }

    [Fact]
    public async Task QueryPageAsync_DoesNotRetryARequestTheServerRejected()
    {
        var handler = new QueuedHttpMessageHandler((HttpStatusCode.NotFound, "{}"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateClient(handler).QueryPageAsync("page-id", CancellationToken.None));

        Assert.Single(handler.RequestUris);
    }

    private static NotionClientService CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.notion.com") });
}
