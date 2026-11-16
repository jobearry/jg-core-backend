using Microsoft.OpenApi;

namespace Generic.API.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
    {
        var contactInfo = new OpenApiContact
        {
            Name = "Jonathan Golimlim",
            Url = new Uri("https://ng-web-resume.vercel.app/")
        };
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Generic API",
                Version = "v1",
                Description = "Generic API",
                Contact = contactInfo
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document),
                    new List<string>()
                }
            });
        });

        return services;
    }
}
