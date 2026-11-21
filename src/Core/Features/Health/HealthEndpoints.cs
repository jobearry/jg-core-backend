using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Core.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/health")
            .WithTags("Health");

        group.MapGet("", () =>
            {
                var assembly = typeof(HealthEndpoints).Assembly;
                var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? assembly.GetName().Version?.ToString()
                    ?? "unknown";

                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                var uptimeSeconds = (int)(DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds;

                return Results.Ok(new
                {
                    status = "Healthy",
                    version,
                    environment,
                    timestamp = DateTimeOffset.UtcNow,
                    uptimeSeconds
                });
            })
            .AllowAnonymous()
            .WithName("GetHealthStatus")
            .WithSummary("Detailed diagnostic status (uptime, version, environment)");

        return endpoints;
    }
}
