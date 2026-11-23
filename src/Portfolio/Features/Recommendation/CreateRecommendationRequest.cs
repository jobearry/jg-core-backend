namespace Portfolio.Features.Recommendation;

/// <summary>
/// The wire shape for creating a testimonial. <c>CareerPosition</c> is optional;
/// <c>null</c> means the recommender's title must not be published.
/// </summary>
public sealed record CreateRecommendationRequest(
    string PersonName,
    string Description,
    string? CareerPosition);
