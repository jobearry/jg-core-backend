using Portfolio.ErrorHandling;
using Microsoft.OpenApi;

namespace Portfolio
{
    public static class ConfigurationSettings
    {
        /// <summary>
        /// Registers everything the temporary HTTP host owns: Swagger, CORS, and the
        /// exception-handling contract.
        /// </summary>
        public static void AddPresentation(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            services.AddSwaggerConfig(configuration);
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
            });
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
