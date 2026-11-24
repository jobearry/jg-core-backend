using System.Net.Http.Headers;
using Microsoft.AspNetCore.RateLimiting;
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

        // Admin writes use a separate DML-only credential. Local development may set
        // both connection strings to the same value; production must not.
        services.AddDbContext<PortfolioWriteContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("JDBConnectionWrite")));

        services.AddScoped<IExperienceRepository, EfExperienceRepository>();
        services.AddScoped<IProjectRepository, EfProjectRepository>();
        services.AddScoped<ISkillRepository, EfSkillRepository>();
        services.AddScoped<ITechStackCategoryRepository, EfTechStackCategoryRepository>();
        services.AddScoped<IRecommendationRepository, EfRecommendationRepository>();

        services.AddScoped<GetExperienceHandler>();
        services.AddScoped<GetProjectHandler>();
        services.AddScoped<GetProjectByIdHandler>();
        services.AddScoped<CreateProjectHandler>();
        services.AddScoped<UpdateProjectHandler>();
        services.AddScoped<DeleteProjectHandler>();
        services.AddScoped<GetSkillHandler>();
        services.AddScoped<GetSkillByIdHandler>();
        services.AddScoped<GetSkillCategoriesHandler>();
        services.AddScoped<CreateSkillHandler>();
        services.AddScoped<UpdateSkillHandler>();
        services.AddScoped<DeleteSkillHandler>();
        services.AddScoped<GetSkillCategoryByIdHandler>();
        services.AddScoped<CreateSkillCategoryHandler>();
        services.AddScoped<UpdateSkillCategoryHandler>();
        services.AddScoped<DeleteSkillCategoryHandler>();
        services.AddScoped<GetRecommendationHandler>();
        services.AddScoped<GetOverviewHandler>();
        services.AddScoped<GetNotionPageHandler>();
        services.AddScoped<GithubHandler>();
        services.AddScoped<GetRecommendationByIdHandler>();
        services.AddScoped<CreateRecommendationHandler>();
        services.AddScoped<UpdateRecommendationHandler>();
        services.AddScoped<DeleteRecommendationHandler>();

        // Server-supplied timestamps go through TimeProvider so the create handler's
        // clock can be replaced in tests.
        services.AddSingleton(TimeProvider.System);

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
    /// Maps the portfolio slices. The policy names are optional so a host without a
    /// rate-limiter middleware can leave them unset; the composed JGBackend host
    /// supplies the passthrough, default and admin-write policies.
    /// </summary>
    public static IEndpointRouteBuilder MapPortfolioFeatures(
        this IEndpointRouteBuilder endpoints,
        string? notionRateLimitPolicy = null,
        string? githubRateLimitPolicy = null,
        string? defaultRateLimitPolicy = null,
        string? adminRateLimitPolicy = null)
    {
        var group = endpoints.MapGroup("");
        if (defaultRateLimitPolicy is not null)
        {
            group.RequireRateLimiting(defaultRateLimitPolicy);
        }

        group.MapNotionEndpoints(notionRateLimitPolicy);
        group.MapGithubEndpoints(githubRateLimitPolicy);
        group.MapExperienceEndpoints();
        group.MapProjectEndpoints(adminRateLimitPolicy);
        group.MapRecommendationEndpoints(adminRateLimitPolicy);
        group.MapSkillEndpoints(adminRateLimitPolicy);
        group.MapOverviewEndpoints();

        return endpoints;
    }
}
