namespace Generic.Features.Auth;

public enum AuthTokenStatus
{
    Success,
    InvalidKey,
    KeyTooShort
}

public sealed record AuthTokenResult(AuthTokenStatus Status, string? Token);

public interface IJwtTokenService
{
    /// <summary>
    /// Validates the provided key against the configured JWT signing key using a
    /// fixed-time comparison, and issues a signed token when it is valid.
    /// </summary>
    AuthTokenResult CreateToken(string? providedKey);
}
