namespace JDB_Portfolio_API.Application.Settings;

/// <summary>
/// Display copy for the <c>/overview</c> tiles, bound from the <c>Overview</c>
/// configuration section. The counts are computed from the repositories; only the
/// text lives here, so changing a sentence is a configuration change, not a deploy.
/// </summary>
public sealed class OverviewSettings
{
    public OverviewItemSettings Projects { get; set; } = new();

    public OverviewItemSettings Skills { get; set; } = new();

    public OverviewItemSettings Recommendations { get; set; } = new();
}

/// <summary>
/// The text of one dashboard tile. A missing section binds to empty strings rather
/// than throwing at startup; the endpoint still returns three tiles in that case.
/// </summary>
public sealed class OverviewItemSettings
{
    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
