namespace JDB_Portfolio_API.Application.DTO;

/// <summary>
/// One counter on the overview. <c>Key</c> is the stable identifier (<c>projects</c>,
/// <c>skills</c>, <c>recommendations</c>); the client owns the label, icon, order and
/// destination link, so adding a counter is additive and reordering them is free.
/// </summary>
public sealed record OverviewItemDto(string Key, int Count);
