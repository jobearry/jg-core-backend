namespace Portfolio.Features.Notion;

/// <summary>
/// Bridges <see cref="IPageContentReader"/> to the Notion client.
/// </summary>
/// <remarks>
/// The translation is intentionally thin: no logic beyond the field-by-field mapping, so
/// upstream exception types survive for the handler to classify.
/// </remarks>
public sealed class NotionPageContentReader : IPageContentReader
{
    private readonly NotionClientService _client;

    public NotionPageContentReader(NotionClientService client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<PageContent>> ReadPageAsync(
        string pageId,
        CancellationToken cancellationToken)
    {
        var pages = await _client.QueryPageAsync(pageId, cancellationToken);

        return pages
            .Select(page => new PageContent(page.Url, page.Date, page.Title))
            .ToList();
    }
}
