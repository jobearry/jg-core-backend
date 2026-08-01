using JDB_Portfolio_API.Application.Settings;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Persistence.Contexts;
using JDB_Portfolio_API.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace JDB_Portfolio_API
{
    public static class ConfigurationSettings
    {
        public static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            // Read connection string from appsettings.json or environment variables
            var jdbConnectionString = configuration.GetConnectionString("JDBConnection");

            //Notion
            services.Configure<NotionSettings>(configuration.GetSection("Notion"));
            services.AddHttpClient<NotionClientService>()
                .ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri("https://api.notion.com");
                });
            services.AddScoped<INotionClientService>(sp => sp.GetRequiredService<NotionClientService>());

            // Register DbContext
            services.AddDbContext<PortfolioContext>(options =>
                options.UseNpgsql(jdbConnectionString)
            );

            // Base DI for repositories
            //services.AddScoped(typeof(IRepository<>), typeof(BasePortfolioRepository<>));

            //// Base DI for services and mappers
            //services.AddScoped(typeof(IService<>), typeof(BasePortfolioService<>));
            //services.AddScoped(typeof(IMappedService<,,>), typeof(BaseMappedPortfolioService<,,>));

            ////DI for experiences section
            //services.AddScoped<IMapper<Experience, DTOExperience, DTOExperienceCreate>, ExperiencesReadMapper>();
            //services.AddScoped<IPortfolioExperienceService, PortfolioExperienceService>();
            //services.AddScoped<IPortfolioExperienceRepository, PortfolioExperienceRepository>();

            ////DI for tech stack section
            //services.AddScoped<IMapper<TechStackDescription, DTOTechStackDescription, DTOTechStackDescription>, TechStackDescriptionMapper>();
            //services.AddScoped<IMapper<TechStackSpec, DTOTechStackSpec, DTOTechStackSpec>, TechStackSpecMapper>();
            //services.AddScoped<ITechStackMapper, TechStackMapper>();
            //services.AddScoped<IPortfolioTechStackService, PortfolioTechStackService>();
        }

        public static void AddSwaggerConfig(this IServiceCollection services, IConfiguration configuration)
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
                    Title = "JDB Portfolio API: Portfolio",
                    Version = "v1",
                    Description = "Portfolio Resource Backend Definition",
                    Contact = contactInfo
                });

                options.SwaggerDoc("v2", new OpenApiInfo
                {
                    Title = "JDB Portfolio API: Notion Integration",
                    Version = "v2",
                    Description = "",
                    Contact = contactInfo
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer"),
                        new List<string>()
                    }
                });
            });
        }

        public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // bind options and register
            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

            var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()!;
            var jwtKey = Encoding.UTF8.GetBytes(jwtOptions.Key);

            // register authorization + authentication using JWT Bearer
            services.AddAuthorization();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.SaveToken = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtOptions.Issuer,
                            ValidAudience = jwtOptions.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
                        };
                    });
        }

        public static void AddCorsOrigins(this IServiceCollection services)
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
        }
    }
}
