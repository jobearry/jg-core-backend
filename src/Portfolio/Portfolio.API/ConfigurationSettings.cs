using Portfolio.Application.Settings;
using Portfolio.Application.Abstractions;
using Portfolio.Adapters;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Net.Http.Headers;
using Portfolio.ExternalService.Notion.Services;
using Portfolio.ExternalService.Notion.Settings;
using Portfolio.ExternalService.Github.Services;
using Portfolio.ExternalService.Github.Settings;
using Portfolio.Application.Services;
using Portfolio.Features.Experience;
using Portfolio.Features.Project;
using Portfolio.Features.Skill;
using Portfolio.Authentication;
using Portfolio.ErrorHandling;
using Portfolio.Persistence.Repositories;
using System.IdentityModel.Tokens.Jwt;

namespace Portfolio
{
    public static class ConfigurationSettings
    {
        /// <summary>
        /// Registers the application use cases. These depend only on the repository and
        /// page-content ports declared in the inner layers.
        /// </summary>
        public static void AddApplication(this IServiceCollection services)
        {
            services.AddScoped<PortfolioService>();
            services.AddScoped<GetExperienceHandler>();
            services.AddScoped<GetProjectHandler>();
            services.AddScoped<PortfolioRecommendationService>();
            services.AddScoped<GetSkillHandler>();
            services.AddScoped<IGithubRepositoryService, GithubRepositoryService>();
        }

        /// <summary>
        /// Registers the persistence adapters: the EF Core context and one repository per
        /// aggregate. Repositories map persistence entities to domain entities with the
        /// hand-written mappers. Ports are declared in Domain; implementations live here.
        /// </summary>
        public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            // Read connection string from appsettings.json or environment variables
            var jdbConnectionString = configuration.GetConnectionString("JDBConnection");

            services.AddDbContext<PortfolioContext>(options =>
                options.UseNpgsql(jdbConnectionString)
            );

            services.AddScoped<IExperienceRepository, EfExperienceRepository>();
            services.AddScoped<IProjectRepository, EfProjectRepository>();
            services.AddScoped<ISkillRepository, EfSkillRepository>();
            services.AddScoped<IPortfolioRecommendationRepository, EfPortfolioRecommendationRepository>();
        }

        /// <summary>
        /// Registers the outbound integrations. The Notion adapter (<see cref="IPageContentReader"/>)
        /// is a presentation concern and is registered by <see cref="AddPresentation"/>.
        /// </summary>
        public static void AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<NotionSettings>(configuration.GetSection("Notion"));
            services.Configure<GithubAPISettings>(configuration.GetSection("Github"));

            // The typed client owns its transport settings: base address, auth headers and
            // a 15s timeout. Keeping them here means NotionClientService knows only the
            // path it calls, and every Notion request has the same limits.
            services.AddHttpClient<NotionClientService>((provider, client) =>
            {
                var notion = provider.GetRequiredService<IOptions<NotionSettings>>().Value;

                client.BaseAddress = new Uri("https://api.notion.com");
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", notion.Token);
                client.DefaultRequestHeaders.Add("Notion-Version", notion.Version);
            });

            services.AddHttpClient("Github", (provider, client) =>
            {
                var github = provider.GetRequiredService<IOptions<GithubAPISettings>>().Value;

                client.BaseAddress = new Uri($"{github.BaseUrl.TrimEnd('/')}/");
                client.Timeout = TimeSpan.FromSeconds(15);
            });
            services.AddTransient(provider =>
                new GithubClientService(
                    provider.GetRequiredService<IHttpClientFactory>().CreateClient("Github"),
                    provider.GetRequiredService<IOptions<GithubAPISettings>>().Value));
        }

        /// <summary>
        /// Registers everything the HTTP host owns: the page-content adapter, Swagger,
        /// authentication, CORS, and the exception-handling contract.
        /// </summary>
        public static void AddPresentation(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            services.AddScoped<IPageContentReader, NotionPageContentReader>();
            services.AddScoped<IGithubRepositoryReader, GithubRepositoryReader>();

            services.AddSwaggerConfig(configuration);
            services.AddAuthentication(configuration);
            services.AddCorsOrigins(configuration, environment);

            // Anything that escapes an endpoint — including exceptions thrown outside a
            // service — becomes the same application/problem+json shape.
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();
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

                // Per-operation security, not a global requirement: the public portfolio
                // endpoints must not be documented as needing a token.
                options.OperationFilter<AuthorizeOperationFilter>();
            });
        }

        public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // bind options and register
            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

            // register authorization + authentication using JWT Bearer
            services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer();

            // Resolve JwtOptions lazily, through the same binding JwtTokenService uses.
            //
            // The previous version read the key *eagerly* here (configuration.GetSection
            // at registration time) and then read it again when signing. Two reads of the
            // same setting is two sources of truth: anything that changed configuration
            // between them — a test host, a reload, a late appsettings layer — produced
            // tokens signed with one key and validated with another, and the symptom was
            // a silent 401 rather than an error.
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
                    {
                        var jwt = jwtOptions.Value;

                        bearer.MapInboundClaims = false;
                        bearer.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwt.Issuer,
                            ValidAudience = jwt.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                            ClockSkew = TimeSpan.Zero,
                            // MapInboundClaims is off, so "sub" is not silently rewritten
                            // to ClaimTypes.NameIdentifier. Without this, User.Identity.Name
                            // is null even though the token carries a subject.
                            NameClaimType = JwtRegisteredClaimNames.Sub
                        };
                    });

            services.AddAuthorization();

            // Token construction is application-adjacent logic with a crypto dependency,
            // so the interface is in Application and the implementation lives here.
            services.AddScoped<IAuthTokenService, JwtTokenService>();
        }

        /// <summary>
        /// The policy name used by <c>app.UseCors</c>. Declared once so the registration and
        /// the pipeline cannot drift apart.
        /// </summary>
        public const string SpaCorsPolicy = "AllowSPA";

        /// <summary>
        /// Allows the browser origins that may read the API.
        /// </summary>
        /// <remarks>
        /// Origins are configuration, not code: <c>Cors:AllowedOrigins</c> ships with the
        /// production domains, so a new domain or a Vercel preview deployment is an
        /// environment variable (<c>Cors__AllowedOrigins__0</c>, <c>__1</c>, …) instead of an
        /// edit plus a redeploy.
        ///
        /// <para>
        /// Two deliberate omissions. <b>No <c>AllowCredentials()</c></b>: bearer tokens travel
        /// in the <c>Authorization</c> header, which CORS treats as an ordinary request header,
        /// and nothing here sets a cookie. Promising every allowed origin that it may send
        /// cookies is a standing invitation that nothing currently accepts. <b>No localhost in
        /// production</b>: <c>Cors:DevelopmentOrigins</c> is applied only when the host is in
        /// the Development environment, so a page served from a local port cannot read the
        /// deployed API.
        /// </para>
        ///
        /// <para>
        /// This is not access control. CORS only tells browsers whether JS from an origin may
        /// read a response; <c>curl</c> and server-side clients ignore it entirely. The
        /// portfolio read endpoints are anonymous on purpose — see
        /// <c>docs/api-reference.md</c> §2 and §6.
        /// </para>
        /// </remarks>
        public static void AddCorsOrigins(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            var allowedOrigins = ReadConfiguredOrigins(configuration, "Cors:AllowedOrigins");

            if (environment.IsDevelopment())
            {
                allowedOrigins.AddRange(ReadConfiguredOrigins(configuration, "Cors:DevelopmentOrigins"));
            }

            var origins = allowedOrigins
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            services.AddCors(options =>
            {
                options.AddPolicy(SpaCorsPolicy, policy =>
                {
                    // Only what the API actually accepts: six reads and one POST. Headers are
                    // narrowed too — the browser sends an Origin header that CORS handles on
                    // its own, so Content-Type and Authorization are the whole surface.
                    policy.WithOrigins(origins)
                          .WithMethods("GET", "POST")
                          .WithHeaders("Content-Type", "Authorization");
                });
            });
        }

        /// <summary>
        /// Reads an origin list from configuration, ignoring blanks. A trailing slash is
        /// stripped because the browser's <c>Origin</c> header never carries one, so an
        /// origin configured as <c>https://example.com/</c> would otherwise never match.
        /// </summary>
        private static List<string> ReadConfiguredOrigins(IConfiguration configuration, string section) =>
            configuration.GetSection(section)
                .Get<string[]>()?
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin.Trim().TrimEnd('/'))
                .ToList()
            ?? new List<string>();
    }
}
