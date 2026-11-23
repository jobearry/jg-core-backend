using System.Threading.RateLimiting;
using Core.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;

namespace JGBackend.Configuration;

public static class RateLimitConfiguration
{
    /// <summary>Coarse per-IP backstop for endpoints without a stricter policy.</summary>
    private const int DefaultPermitLimit = 120;

    /// <summary>Shared limit for the two external-service passthroughs.</summary>
    private const int PassthroughPermitLimit = 30;

    /// <summary>Admin writes are authenticated and rate-limited more tightly than reads.</summary>
    private const int AdminWritePermitLimit = 30;

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddRateLimitingConfig(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            AddFixedWindow(options, RateLimitPolicies.Default, DefaultPermitLimit);
            AddFixedWindow(options, RateLimitPolicies.AuthLogin, 5);
            AddFixedWindow(options, RateLimitPolicies.NotionPassthrough, PassthroughPermitLimit);
            AddFixedWindow(options, RateLimitPolicies.GithubPassthrough, PassthroughPermitLimit);
            AddFixedWindow(options, RateLimitPolicies.AdminWrite, AdminWritePermitLimit);
        });

        return services;
    }

    private static void AddFixedWindow(
        RateLimiterOptions options,
        string policyName,
        int permitLimit) =>
        options.AddPolicy<string>(policyName, httpContext => CreateFixedWindow(httpContext, permitLimit));

    private static RateLimitPartition<string> CreateFixedWindow(HttpContext httpContext, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ResolvePartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = Window,
                QueueLimit = 0
            });

    private static string ResolvePartitionKey(HttpContext httpContext)
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            return "unknown";
        }

        // A caller can surface as ::ffff:203.0.113.5 or 203.0.113.5 depending on the hop.
        // Normalize so one caller always maps to one partition.
        if (remoteIp.IsIPv4MappedToIPv6)
        {
            remoteIp = remoteIp.MapToIPv4();
        }

        return remoteIp.ToString();
    }
}
