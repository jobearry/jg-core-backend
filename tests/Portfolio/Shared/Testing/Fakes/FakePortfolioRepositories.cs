using Portfolio.Features.Notion;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using Portfolio.Features.Experience;
using Portfolio.Features.Project;
using Portfolio.Features.Skill;

namespace Portfolio.Api.Tests.Fakes;

/// <summary>
/// In-memory repositories. The endpoint tests care about the HTTP contract — status
/// code, JSON shape, error mapping — not about SQL. Faking the ports keeps the tests
/// off the database entirely, so they run anywhere and cannot touch real rows.
/// </summary>
public sealed class FakeExperienceRepository : IExperienceRepository
{
    public List<Experience> Experiences { get; } = new();

    /// <summary>When set, every call fails with this exception.</summary>
    public Exception? Failure { get; set; }

    /// <summary>The token the last call received, so a test can assert it flowed.</summary>
    public CancellationToken? LastCancellationToken { get; private set; }

    public Task<List<Experience>> GetAllAsync(CancellationToken cancellationToken) =>
        Respond(cancellationToken);

    public Task<List<Experience>> GetAllWithProjectsAsync(CancellationToken cancellationToken) =>
        Respond(cancellationToken);

    private Task<List<Experience>> Respond(CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;

        return Failure is null
            ? Task.FromResult(Experiences)
            : Task.FromException<List<Experience>>(Failure);
    }
}

public sealed class FakeProjectRepository : IProjectRepository
{
    public List<Project> Projects { get; } = new();

    public Task<List<Project>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Projects);

    public Task<List<Project>> GetAllWithTechStacksAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Projects);
}

public sealed class FakeTechStackRepository : ISkillRepository
{
    public List<TechStack> TechStacks { get; } = new();

    public Exception? Failure { get; set; }

    public Task<List<TechStack>> GetAllAsync(CancellationToken cancellationToken) => Respond();

    private Task<List<TechStack>> Respond() =>
        Failure is null
            ? Task.FromResult(TechStacks)
            : Task.FromException<List<TechStack>>(Failure);
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
