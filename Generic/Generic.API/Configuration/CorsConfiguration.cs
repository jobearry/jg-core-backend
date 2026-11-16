namespace Generic.API.Configuration;

public static class CorsConfiguration
{
    public static IServiceCollection AddCorsOrigins(this IServiceCollection services)
    {
        string[] allowedOrigins =
        {
            "http://localhost:4200",
            "https://jgolimlim-portfolio.vercel.app",
            "https://jgolimlim-web.vercel.app"
        };
        services.AddCors(c =>
        {
            c.AddPolicy("AllowSPA",
                opts => opts.WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials());
        });

        return services;
    }
}
