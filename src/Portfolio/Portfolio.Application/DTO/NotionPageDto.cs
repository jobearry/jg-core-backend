namespace Portfolio.Application.DTO;

/// <summary>
/// The published shape of a Notion page.
/// </summary>
/// <remarks>
/// Kept vendor-named on purpose: the endpoint is a Notion integration
/// (<c>/api/v1/notion/{pageId}</c>, tagged "ExternalServices - Notion"), so the contract
/// names the source. The internal model it is mapped from is the vendor-neutral
/// <see cref="Models.PageContent"/>; this record exists so the wire shape has a name and a
/// schema, not because the application cares which CMS produced the page (plan 3.2).
/// </remarks>
public sealed record NotionPageDto(
    string Url,
    string Date,
    string Title);
