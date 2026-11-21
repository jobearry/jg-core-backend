namespace Portfolio.Features.Notion;

/// <summary>
/// Reads published page content from Notion.
/// </summary>
/// <remarks>
/// The port belongs to this slice: nothing outside it consumes pages, and the adapter
/// that bridges it to the vendor client sits here too.
/// </remarks>
public interface IPageContentReader
{
    Task<IReadOnlyList<PageContent>> ReadPageAsync(string pageId, CancellationToken cancellationToken);
}
