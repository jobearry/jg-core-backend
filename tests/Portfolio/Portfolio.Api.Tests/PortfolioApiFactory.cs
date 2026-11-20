using Portfolio.Api.Tests.Fakes;
using Portfolio.Application.Abstractions;
using Portfolio.Domain.Interfaces;
using Portfolio.Features.Experience;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Portfolio.Api.Tests;

/// <summary>
/// Boots the real application in-process and replaces only the repository ports, so
/// the routing, model binding, serialization and endpoint code under test are the
/// production ones.
///
/// <para>
/// Two settings make this safe:
/// <c>Database:AutoMigrate=false</c> stops the startup migration from touching a
/// database, and the connection string is a placeholder that is never dialled because
/// every repository is faked.
/// </para>
/// </summary>
public sealed class PortfolioApiFactory : WebApplicationFactory<Program>
{
    public FakeExperienceRepository Experience { get; } = new();

    public FakeProjectRepository Projects { get; } = new();

    public FakeTechStackRepository TechStacks { get; } = new();

    public FakeRecommendationRepository Recommendations { get; } = new();

    public FakePageContentReader Notion { get; } = new();

    public FakeGithubRepositoryReader Github { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Never dialled: every repository below is a fake.
                ["ConnectionStrings:JDBConnection"] =
                    "Host=unused;Database=unused;Username=unused;Password=unused",

                // Keeps the startup Migrate() out of the way.
                ["Database:AutoMigrate"] = "false",

                // AddAuthentication() dereferences Jwt, and the key must be >= 32 bytes.
                ["Jwt:Key"] = "api-tests-signing-key-that-is-long-enough",
                ["Jwt:Issuer"] = "api-tests",
                ["Jwt:Audience"] = "api-tests",
                ["Jwt:ExpirationInMinutes"] = "60",

                ["Notion:Token"] = "unused",
                ["Notion:Version"] = "2026-03-11"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExperienceRepository>();
            services.RemoveAll<IPortfolioProjectRepository>();
            services.RemoveAll<IPortfolioTechStackRepository>();
            services.RemoveAll<IPortfolioRecommendationRepository>();
            services.RemoveAll<IPageContentReader>();
            services.RemoveAll<IGithubRepositoryReader>();

            services.AddSingleton<IExperienceRepository>(Experience);
            services.AddSingleton<IPortfolioProjectRepository>(Projects);
            services.AddSingleton<IPortfolioTechStackRepository>(TechStacks);
            services.AddSingleton<IPortfolioRecommendationRepository>(Recommendations);
            services.AddSingleton<IPageContentReader>(Notion);
            services.AddSingleton<IGithubRepositoryReader>(Github);
        });
    }
}
