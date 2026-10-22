using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.Services;
using JDB_Portfolio_API.Application.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JDB_Portfolio_API.Authentication;

/// <summary>
/// Issues the authorisation token for the single shared key.
/// </summary>
/// <remarks>
/// This lives in the presentation project rather than in Application on purpose: it is
/// cryptography against a library, and the dependency direction means Application names
/// the capability (<see cref="IAuthTokenService"/>) while the edge owns the mechanism.
/// It is a separate class — not code in the endpoint — so it can be tested without HTTP.
/// </remarks>
public sealed class JwtTokenService : IAuthTokenService
{
    /// <summary>HS256 signs with a 256-bit key; a shorter secret is unusable.</summary>
    private const int MinimumKeyBytes = 32;

    /// <summary>
    /// The token represents the service itself, not a person: there is exactly one
    /// shared key and no user store. Saying so in <c>sub</c> is more honest than
    /// inventing a username.
    /// </summary>
    private const string Subject = "portfolio-api";

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public Result<string> IssueToken(string providedKey)
    {
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return Result<string>.Failure(Error.Validation("A key is required."));
        }

        if (!KeysMatch(providedKey, _options.Key))
        {
            // Deliberately says nothing about which part was wrong.
            return Result<string>.Failure(
                Error.Unauthorized("The provided key is not valid.", "invalid_credentials"));
        }

        // Checked after the key matches, so an unauthenticated caller cannot use this
        // response to learn the length of the configured secret.
        if (Encoding.UTF8.GetByteCount(_options.Key) < MinimumKeyBytes)
        {
            return Result<string>.Failure(
                Error.Unexpected("Authentication is not configured correctly.", "server_misconfigured"));
        }

        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Subject),
                new Claim(ClaimTypes.Role, "User")
            }),
            Expires = DateTime.UtcNow.AddMinutes(_options.ExpirationInMinutes),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = handler.CreateToken(descriptor);
        return Result<string>.Success(handler.WriteToken(token));
    }

    /// <summary>
    /// Compares in fixed time. A plain <c>==</c> on strings returns at the first
    /// differing byte, which leaks how much of a guess was correct.
    /// </summary>
    private static bool KeysMatch(string provided, string configured)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var configuredBytes = Encoding.UTF8.GetBytes(configured);

        return CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
    }
}
