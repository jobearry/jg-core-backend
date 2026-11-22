using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Core.Features.Auth;

public class JwtTokenService : IJwtTokenService
{
    private const int MinimumKeyBytes = 32;
    private const string Subject = "portfolio-api";

    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public AuthTokenResult CreateToken(string? providedKey)
    {
        var configuredKey = _jwtOptions.Key ?? string.Empty;

        var providedKeyBytes = Encoding.UTF8.GetBytes(providedKey ?? string.Empty);
        var configuredKeyBytes = Encoding.UTF8.GetBytes(configuredKey);

        if (!CryptographicOperations.FixedTimeEquals(providedKeyBytes, configuredKeyBytes))
        {
            return new AuthTokenResult(AuthTokenStatus.InvalidKey, null);
        }

        if (configuredKeyBytes.Length < MinimumKeyBytes)
        {
            return new AuthTokenResult(AuthTokenStatus.KeyTooShort, null);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Subject),
                new Claim("role", "Admin")
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationInMinutes),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(configuredKeyBytes),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new AuthTokenResult(AuthTokenStatus.Success, tokenHandler.WriteToken(token));
    }
}
