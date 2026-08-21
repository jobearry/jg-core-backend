using JDB_Portfolio_API.Application.Settings;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Persistence.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using JDB_Portfolio_API.ExternalService.Notion.Services;
using JDB_Portfolio_API.ExternalService.Notion.Settings;
using JDB_Portfolio_API.Application.Services;
using JDB_Portfolio_API.Persistence.Repositories;
using JDB_Portfolio_API.Persistence.Profiles;

namespace JDB_Portfolio_API
{
    public static class ConfigurationSettings
    {
        public static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            // Read connection string from appsettings.json or environment variables
            var jdbConnectionString = configuration.GetConnectionString("JDBConnection");


            // Register DbContext
            services.AddDbContext<PortfolioContext>(options =>
                options.UseNpgsql(jdbConnectionString)
            );

            services.AddScoped<PortfolioService>();

            ////DI for experiences 
            services.AddScoped<IPortfolioExperienceRepository, EfPortfolioExperienceRepository>();
            services.AddScoped<PortfolioExperienceService>();

            services.AddScoped<IPortfolioProjectRepository, EfPortfolioProjectRepository>();
            services.AddScoped<PortfolioProjectService>();
        }
        public static void AddExternalService(this IServiceCollection services, IConfiguration configuration)
        {

            //Notion
            services.Configure<NotionSettings>(configuration.GetSection("Notion"));
            services.AddHttpClient<NotionClientService>()
                .ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri("https://api.notion.com");
                });

            services.AddScoped<INotionService>(sp => sp.GetRequiredService<NotionClientService>());
        }
        public static void AddMapper(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(a =>
                {

                },
                typeof(PortfolioMappingProfile).Assembly
            );
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
                    Description = "Portfolio Resource Backend",
                    Contact = contactInfo
                });

                options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                {
                    Name = "JWT Authentication",
                    Description = "JWT Authorization header using the Bearer scheme. Input your token here.",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT",
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("bearer", document)] = []
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
            services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtOptions.Issuer,
                            ValidAudience = jwtOptions.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
                            ClockSkew = TimeSpan.Zero
                        };
                    });
            services.AddAuthorization();
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
