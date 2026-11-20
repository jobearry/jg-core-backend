namespace Portfolio.ExternalService.Notion.Models;

/// <summary>
/// A page as Notion describes it: the parser's output, before it is translated into the
/// application's <c>PageContent</c>.
/// </summary>
/// <remarks>
/// Moved here from <c>Domain.ValueObjects</c> by plan 3.2 / KI-045. The shape is
/// deliberately loose — public setters and empty-string defaults — because it mirrors
/// untrusted upstream data and never leaves this project. Validation, if it is ever
/// needed, belongs in the parser, not in a type the core has to trust.
/// </remarks>
public class NotionPage
{
    public string Url { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}
