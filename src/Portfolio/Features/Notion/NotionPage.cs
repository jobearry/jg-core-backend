namespace Portfolio.Features.Notion;

/// <summary>
/// A page as Notion describes it: the parser's output, before it is mapped to
/// <see cref="PageContent"/>.
/// </summary>
/// <remarks>
/// The shape is deliberately loose — public setters and empty-string defaults — because
/// it mirrors untrusted upstream data and never leaves this slice. Validation, if it is
/// ever needed, belongs in the parser.
/// </remarks>
public class NotionPage
{
    public string Url { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}
