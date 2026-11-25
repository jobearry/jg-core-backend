using Microsoft.AspNetCore.Cors.Infrastructure;

namespace JGBackend.Configuration;

public static class CorsConfiguration
{
    /// <summary>
    /// Default policy for anonymous portfolio reads.
    /// </summary>
    public const string SpaCorsPolicy = "AllowSPA";

    /// <summary>
    /// Applied only to the admin group so a browser admin app can send writes.
    /// </summary>
    public const string AdminCorsPolicy = "AllowAdmin";

    public static IServiceCollection AddCorsOrigins(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var allowedOrigins = ReadOrigins(configuration, "Cors:AllowedOrigins");

        if (environment.IsDevelopment())
        {
            allowedOrigins.AddRange(ReadOrigins(configuration, "Cors:DevelopmentOrigins"));
        }

        var origins = allowedOrigins
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var spaMethods = new[] { "GET", "POST" };
        var adminMethods = new[] { "GET", "POST", "PUT", "DELETE" };

        void Apply(CorsPolicyBuilder policy, string[] methods) =>
            policy.WithOrigins(origins)
                  .WithMethods(methods)
                  .WithHeaders("Content-Type", "Authorization");

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => Apply(policy, spaMethods));
            options.AddPolicy(SpaCorsPolicy, policy => Apply(policy, spaMethods));
            options.AddPolicy(AdminCorsPolicy, policy => Apply(policy, adminMethods));
        });

        return services;
    }

    private static List<string> ReadOrigins(IConfiguration configuration, string section) =>
        configuration.GetSection(section)
            .Get<string[]>()?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .ToList()
        ?? new List<string>();
}
