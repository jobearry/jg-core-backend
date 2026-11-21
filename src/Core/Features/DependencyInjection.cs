using Core.Features.Auth;
using Core.Features.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Features;

public static class DependencyInjection
{
    /// <summary>
    /// Registers feature slices (handlers/services per vertical slice).
    /// </summary>
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.AddScoped<Auth.IJwtTokenService, Auth.JwtTokenService>();
        return services;
    }

    /// <summary>
    /// Maps each slice's endpoints. The host calls this once; new slices
    /// extend it here.
    /// </summary>
    public static IEndpointRouteBuilder MapFeatures(
        this IEndpointRouteBuilder endpoints,
        string? defaultRateLimitPolicy = null)
    {
        var group = endpoints.MapGroup("");
        if (defaultRateLimitPolicy is not null)
        {
            group.RequireRateLimiting(defaultRateLimitPolicy);
        }

        group.MapAuthEndpoints();
        group.MapHealthEndpoints();
        return endpoints;
    }
}
