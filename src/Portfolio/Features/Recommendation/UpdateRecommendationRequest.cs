namespace Portfolio.Features.Recommendation;

/// <summary>
/// The wire shape for replacing a testimonial's editable fields.
/// </summary>
public sealed record UpdateRecommendationRequest(
    string PersonName,
    string Description,
    string? CareerPosition);
