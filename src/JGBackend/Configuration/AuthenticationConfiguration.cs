using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Core.Infrastructure.Options;
using System.Text;

namespace JGBackend.Configuration;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // bind options and register
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>();
        if (jwtOptions is null || string.IsNullOrWhiteSpace(jwtOptions.Key))
        {
            throw new InvalidOperationException(
                "JWT configuration is missing. Provide it via environment variables or user secrets " +
                "(Jwt__Key, Jwt__Issuer, Jwt__Audience, Jwt__Expires) or set Jwt in " +
                "appsettings.Development.json. See .env.example for the expected variable names.");
        }

        var jwtKey = Encoding.UTF8.GetBytes(jwtOptions.Key);
        if (jwtKey.Length < 32)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be at least 32 bytes for HmacSha256; got {jwtKey.Length} bytes. " +
                "Set a longer key via Jwt__Key. See .env.example.");
        }

        // register authorization
        services.AddAuthorization();
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            };
        });

        return services;
    }
}
