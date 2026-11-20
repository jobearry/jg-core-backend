namespace JDB_Portfolio_API.Application.DTO;

/// <summary>
/// The published shape of a testimonial. <c>CareerPosition</c> is nullable:
/// <c>null</c> means the recommender would rather not have their title shown.
/// </summary>
public sealed record RecommendationDto(
    int Id,
    string PersonName,
    string Description,
    string? CareerPosition,
    DateTime PostedAt);
