using Generic.Features.Auth;
using Generic.Features.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Generic.Features;

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
    public static IEndpointRouteBuilder MapFeatures(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAuthEndpoints();
        endpoints.MapHealthEndpoints();
        return endpoints;
    }
}
