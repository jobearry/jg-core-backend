namespace JDB_Portfolio_API.Application.Models;

/// <summary>
/// One tile on the dashboard overview: a label, a count and some chrome.
///
/// <para>
/// Plan 3.2 moved this out of <c>Domain.Entities</c>. It has no behaviour and no
/// invariant — nothing in the domain has an opinion about a dashboard tile. It is
/// assembled by <c>PortfolioService.GetOverview</c> from three repository counts and
/// immediately mapped to <c>DashboardItemDto</c>, so it never needed to be a domain type.
/// </para>
/// </summary>
public class DashboardItem
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Description { get; set; } = string.Empty;
}
