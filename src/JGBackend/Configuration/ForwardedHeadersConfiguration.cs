using Microsoft.AspNetCore.HttpOverrides;

namespace JGBackend.Configuration;

public static class ForwardedHeadersConfiguration
{
    /// <summary>
    /// Restores the client IP and scheme forwarded by Azure App Service.
    /// Without this the per-IP rate limiter partitions on the front-end address,
    /// which makes every caller share a single limit.
    /// </summary>
    public static IServiceCollection AddForwardedHeadersConfig(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // App Service front-end addresses are not fixed, so the platform's own
            // header cannot be checked against a static proxy list.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            // Only trust the entry appended by the immediately preceding proxy.
            // This ignores any X-Forwarded-For values a client may have supplied itself.
            options.ForwardLimit = 1;
        });

        return services;
    }
}
