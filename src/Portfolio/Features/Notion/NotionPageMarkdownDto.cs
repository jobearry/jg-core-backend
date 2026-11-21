namespace Portfolio.Features.Notion;

/// <summary>
/// The vendor's markdown response envelope.
/// </summary>
public class NotionPageMarkdownDto
{
    public string Object { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public bool Truncated { get; set; }
    public List<string> UnknownBlockIds { get; set; } = new List<string>();
    public string RequestId { get; set; } = string.Empty;
}
