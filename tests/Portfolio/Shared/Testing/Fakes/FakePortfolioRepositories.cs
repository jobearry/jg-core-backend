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

    public Task<Experience?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;

        return Failure is not null
            ? Task.FromException<Experience?>(Failure)
            : Task.FromResult(Experiences.FirstOrDefault(experience => experience.Id == id));
    }

    public Task<Experience> AddAsync(Experience experience, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (Failure is not null)
        {
            return Task.FromException<Experience>(Failure);
        }

        var created = WithId(
            Experiences.Count == 0 ? 1 : Experiences.Max(item => item.Id) + 1, experience);
        Experiences.Add(created);
        return Task.FromResult(created);
    }

    public Task<Experience> UpdateAsync(Experience experience, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (Failure is not null)
        {
            return Task.FromException<Experience>(Failure);
        }

        var index = Experiences.FindIndex(item => item.Id == experience.Id);
        if (index >= 0)
        {
            Experiences[index] = WithId(experience.Id, experience);
        }

        return Task.FromResult(experience);
    }

    public Task DeleteAsync(Experience experience, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (Failure is not null)
        {
            return Task.FromException(Failure);
        }

        Experiences.RemoveAll(item => item.Id == experience.Id);
        return Task.CompletedTask;
    }

    private static Experience WithId(int id, Experience source)
    {
        var copy = new Experience(id, source.Company, source.Duration, source.Role);
        copy.UpdateDescription(source.Description);
        copy.UpdateResponsibility(source.Responsibility);
        copy.UpdateType(source.Type);
        copy.ReplaceProjects(source.Projects);
        return copy;
    }

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

    public Exception? Failure { get; set; }

    /// <summary>The token the last call received, so a test can assert it flowed.</summary>
    public CancellationToken? LastCancellationToken { get; private set; }

    public Task<List<Project>> GetAllAsync(CancellationToken cancellationToken) => Respond();

    public Task<List<Project>> GetAllWithTechStacksAsync(CancellationToken cancellationToken) =>
        Respond();

    public Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (Failure is not null)
        {
            return Task.FromException<Project?>(Failure);
        }

        return Task.FromResult(Projects.FirstOrDefault(project => project.Id == id));
    }

    public Task<Project> AddAsync(Project project, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<Project>(Failure);
        }

        var created = new Project(
            Projects.Count == 0 ? 1 : Projects.Max(item => item.Id) + 1,
            project.ExperienceId,
            project.ProjectName,
            project.Description,
            project.Contribution,
            project.TechStacks);
        Projects.Add(created);
        return Task.FromResult(created);
    }

    public Task<Project> UpdateAsync(Project project, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException<Project>(Failure);
        }

        var index = Projects.FindIndex(item => item.Id == project.Id);
        if (index >= 0)
        {
            Projects[index] = project;
        }

        return Task.FromResult(project);
    }

    public Task<Project> ReplaceTechStacksAsync(
        Project project, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (Failure is not null)
        {
            return Task.FromException<Project>(Failure);
        }

        var index = Projects.FindIndex(item => item.Id == project.Id);
        if (index < 0)
        {
            return Task.FromException<Project>(
                new InvalidOperationException($"Project {project.Id} does not exist."));
        }

        var updated = new Project(
            project.Id,
            Projects[index].ExperienceId,
            Projects[index].ProjectName,
            Projects[index].Description,
            Projects[index].Contribution,
            project.TechStacks);
        Projects[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteAsync(Project project, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException(Failure);
        }

        Projects.RemoveAll(item => item.Id == project.Id);
        return Task.CompletedTask;
    }

    private Task<List<Project>> Respond() =>
        Failure is null
            ? Task.FromResult(Projects)
            : Task.FromException<List<Project>>(Failure);
}

public sealed class FakeTechStackRepository : ISkillRepository
{
    public List<TechStack> TechStacks { get; } = new();

    public Exception? Failure { get; set; }

    public bool IsUsedByProjects { get; set; }

    /// <summary>The token the last call received, so a test can assert it flowed.</summary>
    public CancellationToken? LastCancellationToken { get; private set; }

    public Task<List<TechStack>> GetAllAsync(CancellationToken cancellationToken) => Respond();

    public Task<TechStack?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
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
