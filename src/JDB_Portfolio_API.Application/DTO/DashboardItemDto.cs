namespace JDB_Portfolio_API.Application.DTO;

/// <summary>One tile on the dashboard overview.</summary>
public sealed record DashboardItemDto(
    string Title,
    string Subtitle,
    string Icon,
    int Count,
    string Description);
