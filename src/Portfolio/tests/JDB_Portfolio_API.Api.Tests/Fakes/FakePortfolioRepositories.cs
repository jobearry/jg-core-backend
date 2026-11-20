using JDB_Portfolio_API.Application.Abstractions;
using JDB_Portfolio_API.Application.Models;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Domain.ValueObjects;
using System.Threading;

namespace JDB_Portfolio_API.Api.Tests.Fakes;

/// <summary>
/// In-memory repositories. The endpoint tests care about the HTTP contract — status
/// code, JSON shape, error mapping — not about SQL. Faking the ports keeps the tests
/// off the database entirely, so they run anywhere and cannot touch real rows.
/// </summary>
public sealed class FakeExperienceRepository : IPortfolioExperienceRepository
{
    public List<Experience> Experiences { get; } = new();

    /// <summary>When set, every call fails with this exception.</summary>
    public Exception? Failure { get; set; }

    /// <summary>The token the last call received, so a test can assert it flowed.</summary>
    public CancellationToken? LastCancellationToken { get; private set; }

    public Task<List<Experience>> GetAllAsync(CancellationToken cancellationToken) =>
        Respond(cancellationToken);

    public Task<List<Experience>> GetAllExperienceWithProjectsAsync(CancellationToken cancellationToken) =>
        Respond(cancellationToken);

    private Task<List<Experience>> Respond(CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;

        return Failure is null
            ? Task.FromResult(Experiences)
            : Task.FromException<List<Experience>>(Failure);
    }
}

/// <summary>
/// Simulates a database failure. <see cref="System.Data.Common.DbException"/> has
/// protected constructors, so subclassing is the only way to create one without taking
/// a dependency on Npgsql in the test project.
/// </summary>
public sealed class FakeDbException : System.Data.Common.DbException
{
    public FakeDbException(string message) : base(message) { }
}

public sealed class FakeProjectRepository : IPortfolioProjectRepository
{
    public List<Project> Projects { get; } = new();

    public Task<List<Project>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Projects);

    public Task<List<Project>> GetAllProjectsWithTechStacksAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Projects);
}

public sealed class FakeTechStackRepository : IPortfolioTechStackRepository
{
    public List<TechStack> TechStacks { get; } = new();

    public Exception? Failure { get; set; }

    public Task<List<TechStack>> GetAllAsync(CancellationToken cancellationToken) => Respond();

    public Task<List<TechStack>> GetAllTechStacksAsync(CancellationToken cancellationToken) => Respond();

    private Task<List<TechStack>> Respond() =>
        Failure is null
            ? Task.FromResult(TechStacks)
            : Task.FromException<List<TechStack>>(Failure);
}

public sealed class FakeRecommendationRepository : IPortfolioRecommendationRepository
{
    public List<Recommendation> Recommendations { get; } = new();

    public Task<List<Recommendation>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Recommendations);

    public Task<List<Recommendation>> GetAllRecommendationsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Recommendations);
}

/// <summary>
/// Stands in for the page reader so the upstream failure path can be tested without a
/// network call. It implements the application port, which is what the endpoint depends
/// on (plan 3.2 / KI-045) — not the Notion client, which the test project cannot mirror.
/// </summary>
public sealed class FakePageContentReader : IPageContentReader
{
    public List<PageContent> Pages { get; } = new();

    public Exception? Failure { get; set; }

    public Task<IReadOnlyList<PageContent>> ReadPageAsync(string pageId, CancellationToken cancellationToken) =>
        Failure is null
            ? Task.FromResult<IReadOnlyList<PageContent>>(Pages)
            : Task.FromException<IReadOnlyList<PageContent>>(Failure);
}
