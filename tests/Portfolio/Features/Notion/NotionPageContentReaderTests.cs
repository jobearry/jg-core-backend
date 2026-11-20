using System.Net;
using System.Text;
using System.Text.Json;
using Portfolio.Features.Notion;

namespace Portfolio.Api.Tests;

/// <summary>
/// Plan 3.2 / KI-045. The endpoint tests replace <c>IPageContentReader</c> with a fake, so
/// they never exercise the adapter that actually sits between the application port and the
/// Notion client. This is the only place the real chain runs:
/// HTTP JSON → parser → vendor model → adapter → <see cref="PageContent"/>.
/// </summary>
public class NotionPageContentReaderTests
{
    [Fact]
    public async Task ReadPageAsync_MapsTheVendorPageOntoPageContent()
    {
        var reader = CreateReader(HttpStatusCode.OK, MarkdownResponse);

        var pages = await reader.ReadPageAsync("page-id", CancellationToken.None);

        var page = Assert.Single(pages);
        Assert.IsType<PageContent>(page);
        Assert.Equal("https://notion.so/page", page.Url);
        Assert.Equal("2026-06-27", page.Date);
        Assert.Equal("About me", page.Title);
    }

    /// <summary>
    /// The adapter must not swallow or rewrap the upstream failure: the Application layer
    /// classifies by exception type to choose between <c>notion_unavailable</c> and
    /// <c>notion_invalid_response</c>. Wrapping it here would silently collapse the two.
    /// </summary>
    [Fact]
    public async Task ReadPageAsync_WhenUpstreamFails_LetsTheExceptionEscape()
    {
        var reader = CreateReader(HttpStatusCode.InternalServerError, "{}");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => reader.ReadPageAsync("page-id", CancellationToken.None));
    }

    private const string MarkdownResponse =
        """{"object":"page","id":"page-id","markdown":"<page url=\"https://notion.so/page\"><mention-date start=\"2026-06-27\"/>About me</page>","truncated":false,"unknownBlockIds":[],"requestId":"req"}""";

    private static NotionPageContentReader CreateReader(HttpStatusCode status, string body)
    {
        var http = new HttpClient(new StubHandler(status, body))
        {
            BaseAddress = new Uri("https://api.notion.com")
        };
        var client = new NotionClientService(http);

        return new NotionPageContentReader(client);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
    }
}
