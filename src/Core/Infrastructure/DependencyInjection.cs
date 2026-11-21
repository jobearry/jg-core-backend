using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure concerns (configuration binding, external clients,
    /// persistence, cross-cutting middleware helpers, etc.). New cross-cutting
    /// services should be wired in here.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Options.JwtOptions>(configuration.GetSection("Jwt"));

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}
