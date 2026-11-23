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

    public bool IsUsedByProjects { get; set; }

    public Task<List<TechStack>> GetAllAsync(CancellationToken cancellationToken) => Respond();

    public Task<TechStack?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<TechStack?>(Failure);
        }

        return Task.FromResult(TechStacks.FirstOrDefault(stack => stack.Id == id));
    }

    public Task<TechStack> AddAsync(
        TechStack stack, int categoryId, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<TechStack>(Failure);
        }

        var created = new TechStack(
            TechStacks.Count == 0 ? 1 : TechStacks.Max(item => item.Id) + 1,
            stack.StackName,
            stack.ImgSrc,
            stack.Category);
        TechStacks.Add(created);
        return Task.FromResult(created);
    }

    public Task<TechStack> UpdateAsync(
        TechStack stack, int categoryId, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<TechStack>(Failure);
        }

        var index = TechStacks.FindIndex(item => item.Id == stack.Id);
        if (index >= 0)
        {
            TechStacks[index] = stack;
        }

        return Task.FromResult(stack);
    }

    public Task<bool> IsUsedByProjectsAsync(int stackId, CancellationToken cancellationToken) =>
        Failure is null
            ? Task.FromResult(IsUsedByProjects)
            : Task.FromException<bool>(Failure);

    public Task DeleteAsync(TechStack stack, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException(Failure);
        }

        TechStacks.RemoveAll(item => item.Id == stack.Id);
        return Task.CompletedTask;
    }

    private Task<List<TechStack>> Respond() =>
        Failure is null
            ? Task.FromResult(TechStacks)
            : Task.FromException<List<TechStack>>(Failure);
}

public sealed class FakeTechStackCategoryRepository : ITechStackCategoryRepository
{
    public List<TechStackCategory> Categories { get; } = new();

    public Exception? Failure { get; set; }

    public bool HasStacks { get; set; }

    public Task<List<TechStackCategory>> GetAllAsync(CancellationToken cancellationToken) =>
        Failure is null
            ? Task.FromResult(Categories)
            : Task.FromException<List<TechStackCategory>>(Failure);

    public Task<TechStackCategory?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<TechStackCategory?>(Failure);
        }

        return Task.FromResult(Categories.FirstOrDefault(category => category.Id == id));
    }

    public Task<TechStackCategory?> GetByNameAsync(
        string category, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<TechStackCategory?>(Failure);
        }

        return Task.FromResult(Categories.FirstOrDefault(item => item.Category == category));
    }

    public Task<TechStackCategory> AddAsync(
        TechStackCategory category, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<TechStackCategory>(Failure);
        }

        var created = new TechStackCategory(
            Categories.Count == 0 ? 1 : Categories.Max(item => item.Id) + 1,
            category.Category);
        Categories.Add(created);
        return Task.FromResult(created);
    }

    public Task<TechStackCategory> UpdateAsync(
        TechStackCategory category, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<TechStackCategory>(Failure);
        }

        var index = Categories.FindIndex(item => item.Id == category.Id);
        if (index >= 0)
        {
            Categories[index] = category;
        }

        return Task.FromResult(category);
    }

    public Task<bool> HasStacksAsync(int categoryId, CancellationToken cancellationToken) =>
        Failure is null
            ? Task.FromResult(HasStacks)
            : Task.FromException<bool>(Failure);

    public Task DeleteAsync(TechStackCategory category, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException(Failure);
        }

        Categories.RemoveAll(item => item.Id == category.Id);
        return Task.CompletedTask;
    }
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
