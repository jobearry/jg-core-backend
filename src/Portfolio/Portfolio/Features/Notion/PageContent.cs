namespace Portfolio.Features.Notion;

/// <summary>
/// One published page read from Notion, before it is mapped to the wire DTO.
/// </summary>
/// <remarks>
/// The fields stay <c>string</c> on purpose. They are untrusted upstream values that are
/// published verbatim; parsing them into <c>Uri</c>/<c>DateOnly</c> here would turn a page
/// whose date reads "Spring 2024" into a <c>502</c>.
/// </remarks>
public sealed record PageContent(string Url, string Date, string Title);
