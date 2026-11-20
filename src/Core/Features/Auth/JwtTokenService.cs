using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Core.Infrastructure.Options;

namespace Core.Features.Auth;

public class JwtTokenService : IJwtTokenService
{
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

        if (configuredKeyBytes.Length < 32)
        {
            return new AuthTokenResult(AuthTokenStatus.KeyTooShort, null);
        }

        var jwtKey = Encoding.ASCII.GetBytes(configuredKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Name", "Jonathan Rodel"),
                new Claim(ClaimTypes.Role, "User")
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.Expires),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(jwtKey),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new AuthTokenResult(AuthTokenStatus.Success, tokenHandler.WriteToken(token));
    }
}
