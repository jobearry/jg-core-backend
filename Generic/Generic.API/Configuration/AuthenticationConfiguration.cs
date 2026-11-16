using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Generic.Infra.Options;
using System.Text;

namespace Generic.API.Configuration;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // bind options and register
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()!;
        var jwtKey = Encoding.UTF8.GetBytes(jwtOptions.Key);

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
