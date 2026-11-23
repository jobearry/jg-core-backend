using System.Net.Http.Headers;
using Core.Features.Auth;
using Core.Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Features.Experience;
using Portfolio.Features.Github;
using Portfolio.Features.Notion;
using Portfolio.Features.Project;
using Portfolio.Features.Recommendation;
using Portfolio.Features.Skill;

namespace Portfolio.Api.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class JGBackendHostCollection
{
    public const string Name = "JGBackend host";
}

/// <summary>
/// Boots the one host in-process and replaces only the repository and reader ports,
/// so routing, model binding, serialization and endpoint code are the production ones.
/// The connection string is a placeholder that is never dialled; startup migration is
/// disabled so no test touches a database.
/// </summary>
/// <remarks>
/// Environment variables are used because the top-level <c>Program</c> reads JWT
/// configuration while the service collection is being built, before a test host can
/// add later configuration sources. The production host only loads User Secrets in
/// Development, so these values are not shadowed.
/// </remarks>
public class JGBackendApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "host-tests-signing-key-that-is-long-enough";

    private const string JwtIssuer = "host-composition-tests";
    private const string JwtAudience = "host-composition-tests";
    private const int JwtExpirationInMinutes = 60;

    private static readonly Dictionary<string, string> Settings = new()
    {
        ["ConnectionStrings__JDBConnection"] =
            "Host=unused;Database=unused;Username=unused;Password=unused",
        ["ConnectionStrings__JDBConnectionWrite"] =
            "Host=unused;Database=unused;Username=unused;Password=unused",
        ["Database__AutoMigrate"] = "false",
        ["Jwt__Key"] = SigningKey,
        ["Jwt__Issuer"] = JwtIssuer,
        ["Jwt__Audience"] = JwtAudience,
        ["Jwt__ExpirationInMinutes"] = JwtExpirationInMinutes.ToString(),
        // Off by default, like Production. Tests that need Swagger derive a factory
        // which turns it on.
        ["Swagger__Enabled"] = "false",
        ["Github__BaseUrl"] = "https://api.github.com",
        ["Notion__Version"] = "2026-03-11"
    };

    private readonly Dictionary<string, string?> _originals;

    public JGBackendApiFactory()
    {
        _originals = Settings.Keys.ToDictionary(
            key => key,
            key => Environment.GetEnvironmentVariable(key));

        foreach (var setting in Settings)
        {
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }

    /// <summary>
    /// A client carrying the test Admin token. The token is built directly because
    /// <c>/auth/login</c> is limited to five requests per minute, which a suite of
    /// endpoint tests would quickly exhaust.
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = SigningKey,
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            ExpirationInMinutes = JwtExpirationInMinutes
        });
        var result = new JwtTokenService(options).CreateToken(SigningKey);
        Assert.NotNull(result.Token);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result.Token);
        return client;
    }

    public FakeExperienceRepository Experience { get; } = new();

    public FakeProjectRepository Projects { get; } = new();

    public FakeTechStackRepository TechStacks { get; } = new();

    public FakeTechStackCategoryRepository SkillCategories { get; } = new();

    public FakeRecommendationRepository Recommendations { get; } = new();

    public FakePageContentReader Notion { get; } = new();

    public FakeGithubRepositoryReader Github { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExperienceRepository>();
            services.RemoveAll<IProjectRepository>();
            services.RemoveAll<ISkillRepository>();
            services.RemoveAll<ITechStackCategoryRepository>();
            services.RemoveAll<IRecommendationRepository>();
            services.RemoveAll<IPageContentReader>();
            services.RemoveAll<IGithubRepositoryReader>();

            services.AddSingleton<IExperienceRepository>(Experience);
            services.AddSingleton<IProjectRepository>(Projects);
            services.AddSingleton<ISkillRepository>(TechStacks);
            services.AddSingleton<ITechStackCategoryRepository>(SkillCategories);
            services.AddSingleton<IRecommendationRepository>(Recommendations);
            services.AddSingleton<IPageContentReader>(Notion);
            services.AddSingleton<IGithubRepositoryReader>(Github);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var original in _originals)
            {
                Environment.SetEnvironmentVariable(original.Key, original.Value);
            }
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// The host gates Swagger in Production behind <c>Swagger:Enabled</c>. Set it for the
/// tests that read the document; the base factory leaves it off.
/// </summary>
public sealed class SwaggerEnabledApiFactory : JGBackendApiFactory
{
    public SwaggerEnabledApiFactory() =>
        Environment.SetEnvironmentVariable("Swagger__Enabled", "true");
}
