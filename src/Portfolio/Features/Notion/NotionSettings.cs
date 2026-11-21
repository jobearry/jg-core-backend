namespace Portfolio.Features.Notion;

/// <summary>
/// Notion credentials and API version, bound from configuration.
/// </summary>
public class NotionSettings
{
    public string Token { get; set; } = string.Empty;
    public string Version { get; set; } = "2026-03-11";
}
