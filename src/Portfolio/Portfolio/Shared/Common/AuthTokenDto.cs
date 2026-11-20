namespace Portfolio.Application.DTO;

/// <summary>
/// The login response. A named type instead of an anonymous object so the shape is
/// documented in Swagger and guarded by a test — plan 2.1's rule that endpoints return
/// DTOs, applied to the one place that slipped through.
/// </summary>
public sealed record AuthTokenDto(string Token);
