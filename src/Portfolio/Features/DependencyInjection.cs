using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portfolio.Features.Experience;
using Portfolio.Features.Github;
using Portfolio.Features.Notion;
using Portfolio.Features.Overview;
using Portfolio.Features.Project;
using Portfolio.Features.Recommendation;
using Portfolio.Features.Skill;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Repositories;

namespace Portfolio.Features;

/// <summary>
/// Composition for the portfolio feature slices. Keeping registration and route
/// mapping here lets a host add the whole module with two calls.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPortfolioFeatures(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PortfolioContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("JDBConnection")));

        services.AddScoped<IExperienceRepository, EfExperienceRepository>();
        services.AddScoped<IProjectRepository, EfProjectRepository>();
        services.AddScoped<ISkillRepository, EfSkillRepository>();
        services.AddScoped<IRecommendationRepository, EfRecommendationRepository>();

        services.AddScoped<GetExperienceHandler>();
        services.AddScoped<GetProjectHandler>();
        services.AddScoped<GetSkillHandler>();
        services.AddScoped<GetRecommendationHandler>();
        services.AddScoped<GetOverviewHandler>();
        services.AddScoped<GetNotionPageHandler>();
        services.AddScoped<GithubHandler>();

        services.AddScoped<IPageContentReader, NotionPageContentReader>();
        services.AddScoped<IGithubRepositoryReader, GithubRepositoryReader>();

        services.Configure<NotionSettings>(configuration.GetSection("Notion"));
        services.Configure<GithubAPISettings>(configuration.GetSection("Github"));

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

        return services;
    }

    /// <summary>
    /// Maps the portfolio slices. A host that runs a rate-limiter middleware can pass
    /// policy names for the external-service passthroughs; the temporary Portfolio host
    /// has no such middleware and leaves them unset.
    /// </summary>
    public static IEndpointRouteBuilder MapPortfolioFeatures(
        this IEndpointRouteBuilder endpoints,
        string? notionRateLimitPolicy = null,
        string? githubRateLimitPolicy = null)
    {
        endpoints.MapNotionEndpoints(notionRateLimitPolicy);
        endpoints.MapGithubEndpoints(githubRateLimitPolicy);
        endpoints.MapExperienceEndpoints();
        endpoints.MapProjectEndpoints();
        endpoints.MapRecommendationEndpoints();
        endpoints.MapSkillEndpoints();
        endpoints.MapOverviewEndpoints();

        return endpoints;
    }
}
