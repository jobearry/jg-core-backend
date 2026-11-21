namespace JGBackend.Configuration;

public static class CorsConfiguration
{
    /// <summary>
    /// Policy name shared by registration and the middleware pipeline.
    /// </summary>
    public const string SpaCorsPolicy = "AllowSPA";

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

        services.AddCors(options =>
        {
            options.AddPolicy(SpaCorsPolicy, policy =>
            {
                policy.WithOrigins(origins)
                      .WithMethods("GET", "POST")
                      .WithHeaders("Content-Type", "Authorization");
            });
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
