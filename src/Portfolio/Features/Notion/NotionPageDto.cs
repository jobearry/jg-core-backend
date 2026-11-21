namespace Portfolio.Features.Notion;

/// <summary>
/// The published shape of a Notion page.
/// </summary>
/// <remarks>
/// Kept vendor-named on purpose: the endpoint is a Notion integration
/// (<c>/api/v1/notion/{pageId}</c>, tagged "ExternalServices - Notion"), so the contract
/// names the source. The internal model it is mapped from is <see cref="PageContent"/>.
/// </remarks>
public sealed record NotionPageDto(
    string Url,
    string Date,
    string Title);
