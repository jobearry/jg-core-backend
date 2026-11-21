using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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
/// add later configuration sources.
/// </remarks>
public class JGBackendApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "host-tests-signing-key-that-is-long-enough";

    private static readonly Dictionary<string, string> Settings = new()
    {
        ["ConnectionStrings__JDBConnection"] =
            "Host=unused;Database=unused;Username=unused;Password=unused",
        ["Database__AutoMigrate"] = "false",
        ["Jwt__Key"] = SigningKey,
        ["Jwt__Issuer"] = "host-composition-tests",
        ["Jwt__Audience"] = "host-composition-tests",
        ["Jwt__ExpirationInMinutes"] = "60",
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

    public FakeExperienceRepository Experience { get; } = new();

    public FakeProjectRepository Projects { get; } = new();

    public FakeTechStackRepository TechStacks { get; } = new();

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
            services.RemoveAll<IRecommendationRepository>();
            services.RemoveAll<IPageContentReader>();
            services.RemoveAll<IGithubRepositoryReader>();

            services.AddSingleton<IExperienceRepository>(Experience);
            services.AddSingleton<IProjectRepository>(Projects);
            services.AddSingleton<ISkillRepository>(TechStacks);
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
