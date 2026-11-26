using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Application.Common;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project.Tests;

/// <summary>
/// The join write resolves ids through the Skill read port before touching the
/// Project aggregate, so names, images and categories always come from the shared row.
/// </summary>
public class ProjectTechStackHandlerTests
{
    [Fact]
    public async Task ReplaceTechStacks_WithValidIds_UsesResolvedSnapshots()
    {
        var (projects, stacks) = Seeded();
        var handler = new ReplaceProjectTechStacksHandler(
            projects, stacks, NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new ReplaceProjectTechStacksRequest([2, 5]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dtoStacks = result.Value!.TechStacks;
        Assert.Equal(2, dtoStacks.Count);
        Assert.Equal(2, dtoStacks[0].Id);
        Assert.Equal("C#", dtoStacks[0].StackName);
        Assert.Equal("https://example.test/csharp.svg", dtoStacks[0].ImgSrc);
        Assert.Equal("Backend", dtoStacks[0].Category);
        Assert.Equal("SQL", dtoStacks[1].StackName);
        Assert.Equal("Data", dtoStacks[1].Category);

        var stored = Assert.Single(projects.Projects);
        Assert.Equal(new[] { 2, 5 }, stored.TechStacks.Select(stack => stack.Id));
    }

    [Fact]
    public async Task ReplaceTechStacks_WithEmptyList_ClearsTheStacks()
    {
        var (projects, stacks) = Seeded();
        var handler = new ReplaceProjectTechStacksHandler(
            projects, stacks, NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new ReplaceProjectTechStacksRequest([]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.TechStacks);
        Assert.Empty(Assert.Single(projects.Projects).TechStacks);
    }

    [Fact]
    public async Task ReplaceTechStacks_WithNullRequest_ReturnsValidationError()
    {
        var (projects, stacks) = Seeded();
        var handler = new ReplaceProjectTechStacksHandler(
            projects, stacks, NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(7, null, CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Equal("Old", Assert.Single(projects.Projects).TechStacks[0].StackName);
    }

    [Fact]
    public async Task ReplaceTechStacks_WithNullIdList_ReturnsValidationError()
    {
        var handler = new ReplaceProjectTechStacksHandler(
            new FakeProjectRepository(),
            new FakeTechStackRepository(),
            NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new ReplaceProjectTechStacksRequest(null), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReplaceTechStacks_WithNonPositiveStackId_ReturnsValidationError(int stackId)
    {
        var (projects, stacks) = Seeded();
        var handler = new ReplaceProjectTechStacksHandler(
            projects, stacks, NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new ReplaceProjectTechStacksRequest([stackId]), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Equal("Old", Assert.Single(projects.Projects).TechStacks[0].StackName);
    }

    [Fact]
    public async Task ReplaceTechStacks_WithDuplicateIds_ReturnsValidationError()
    {
        var (projects, stacks) = Seeded();
        var handler = new ReplaceProjectTechStacksHandler(
            projects, stacks, NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new ReplaceProjectTechStacksRequest([2, 2]), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Equal("Old", Assert.Single(projects.Projects).TechStacks[0].StackName);
    }

    [Fact]
    public async Task ReplaceTechStacks_WithUnknownStackId_ReturnsValidationError()
    {
        var (projects, stacks) = Seeded();
        var handler = new ReplaceProjectTechStacksHandler(
            projects, stacks, NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new ReplaceProjectTechStacksRequest([2, 99]), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Equal("Old", Assert.Single(projects.Projects).TechStacks[0].StackName);
    }

    [Fact]
    public async Task ReplaceTechStacks_WithInvalidProjectId_ReturnsValidationError()
    {
        var handler = new ReplaceProjectTechStacksHandler(
            new FakeProjectRepository(),
            new FakeTechStackRepository(),
            NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(
            0, new ReplaceProjectTechStacksRequest([2]), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task ReplaceTechStacks_ForMissingProject_ReturnsNotFound()
    {
        var (_, stacks) = Seeded();
        var handler = new ReplaceProjectTechStacksHandler(
            new FakeProjectRepository(), stacks,
            NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(
            99, new ReplaceProjectTechStacksRequest([2]), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task ReplaceTechStacks_WhenTheProjectReadFails_ReturnsInfrastructureError()
    {
        var (projects, stacks) = Seeded();
        projects.Failure = new FakeDbException("could not connect");
        var handler = new ReplaceProjectTechStacksHandler(
            projects, stacks, NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new ReplaceProjectTechStacksRequest([2]), CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
    }

    [Fact]
    public async Task ReplaceTechStacks_WhenTheSkillReadFails_ReturnsInfrastructureError()
    {
        var (projects, stacks) = Seeded();
        stacks.Failure = new FakeDbException("could not connect");
        var handler = new ReplaceProjectTechStacksHandler(
            projects, stacks, NullLogger<ReplaceProjectTechStacksHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new ReplaceProjectTechStacksRequest([2]), CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
    }

    [Fact]
    public async Task ReplaceTechStacks_PassesTheTokenToBothRepositories()
    {
        var (projects, stacks) = Seeded();
        var handler = new ReplaceProjectTechStacksHandler(
            projects, stacks, NullLogger<ReplaceProjectTechStacksHandler>.Instance);
        using var source = new CancellationTokenSource();

        await handler.HandleAsync(
            7, new ReplaceProjectTechStacksRequest([2]), source.Token);

        Assert.Equal(source.Token, projects.LastCancellationToken);
        Assert.Equal(source.Token, stacks.LastCancellationToken);
    }

    // Incremental add/remove

    [Fact]
    public async Task AddTechStack_WithValidId_AddsLink()
    {
        var (projects, stacks) = Seeded();
        var handler = new AddProjectTechStackHandler(
            projects, stacks, NullLogger<AddProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new AddProjectTechStackRequest(5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TechStacks.Count);
        var stored = Assert.Single(projects.Projects);
        Assert.Contains(5, stored.TechStacks.Select(r => r.Id));
    }

    [Fact]
    public async Task AddTechStack_AlreadyLinked_ReturnsValidationError()
    {
        var (projects, stacks) = Seeded();
        var handler = new AddProjectTechStackHandler(
            projects, stacks, NullLogger<AddProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new AddProjectTechStackRequest(2), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.True(projects.Projects.Single().TechStacks.Count == 1);
    }

    [Fact]
    public async Task AddTechStack_UnknownStack_ReturnsNotFound()
    {
        var (projects, stacks) = Seeded();
        var handler = new AddProjectTechStackHandler(
            projects, stacks, NullLogger<AddProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new AddProjectTechStackRequest(99), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddTechStack_WithNonPositiveStackId_ReturnsValidationError(int stackId)
    {
        var (projects, stacks) = Seeded();
        var handler = new AddProjectTechStackHandler(
            projects, stacks, NullLogger<AddProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new AddProjectTechStackRequest(stackId), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task AddTechStack_WithInvalidProjectId_ReturnsValidationError()
    {
        var handler = new AddProjectTechStackHandler(
            new FakeProjectRepository(), new FakeTechStackRepository(),
            NullLogger<AddProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            0, new AddProjectTechStackRequest(2), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task AddTechStack_ForMissingProject_ReturnsNotFound()
    {
        var (_, stacks) = Seeded();
        var handler = new AddProjectTechStackHandler(
            new FakeProjectRepository(), stacks, NullLogger<AddProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            99, new AddProjectTechStackRequest(2), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task AddTechStack_WhenProjectReadFails_ReturnsInfrastructureError()
    {
        var (projects, stacks) = Seeded();
        projects.Failure = new FakeDbException("could not connect");
        var handler = new AddProjectTechStackHandler(
            projects, stacks, NullLogger<AddProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new AddProjectTechStackRequest(2), CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
    }

    [Fact]
    public async Task AddTechStack_WhenSkillReadFails_ReturnsInfrastructureError()
    {
        var (projects, stacks) = Seeded();
        stacks.Failure = new FakeDbException("could not connect");
        var handler = new AddProjectTechStackHandler(
            projects, stacks, NullLogger<AddProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new AddProjectTechStackRequest(2), CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
    }

    [Fact]
    public async Task RemoveTechStack_WithValidId_RemovesLink()
    {
        var (projects, stacks) = Seeded();
        var handler = new RemoveProjectTechStackHandler(
            projects, NullLogger<RemoveProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new RemoveProjectTechStackRequest(2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.TechStacks);
        Assert.Empty(Assert.Single(projects.Projects).TechStacks);
    }

    [Fact]
    public async Task RemoveTechStack_NotLinked_ReturnsNotFound()
    {
        var (projects, stacks) = Seeded();
        var handler = new RemoveProjectTechStackHandler(
            projects, NullLogger<RemoveProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new RemoveProjectTechStackRequest(5), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RemoveTechStack_WithNonPositiveStackId_ReturnsValidationError(int stackId)
    {
        var (projects, stacks) = Seeded();
        var handler = new RemoveProjectTechStackHandler(
            projects, NullLogger<RemoveProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new RemoveProjectTechStackRequest(stackId), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task RemoveTechStack_WithInvalidProjectId_ReturnsValidationError()
    {
        var handler = new RemoveProjectTechStackHandler(
            new FakeProjectRepository(), NullLogger<RemoveProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            0, new RemoveProjectTechStackRequest(2), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task RemoveTechStack_ForMissingProject_ReturnsNotFound()
    {
        var handler = new RemoveProjectTechStackHandler(
            new FakeProjectRepository(), NullLogger<RemoveProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            99, new RemoveProjectTechStackRequest(2), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task RemoveTechStack_WhenProjectReadFails_ReturnsInfrastructureError()
    {
        var (projects, stacks) = Seeded();
        projects.Failure = new FakeDbException("could not connect");
        var handler = new RemoveProjectTechStackHandler(
            projects, NullLogger<RemoveProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new RemoveProjectTechStackRequest(2), CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
    }

    [Fact]
    public async Task RemoveTechStack_WhenProjectWriteFails_ReturnsInfrastructureError()
    {
        var (projects, stacks) = Seeded();
        projects.Failure = new FakeDbException("could not connect");
        var handler = new RemoveProjectTechStackHandler(
            projects, NullLogger<RemoveProjectTechStackHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new RemoveProjectTechStackRequest(2), CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
    }

    private static (FakeProjectRepository Projects, FakeTechStackRepository Stacks) Seeded()
    {
        var projects = new FakeProjectRepository();
        projects.Projects.Add(new DomainProject(
            7,
            null,
            "Portfolio API",
            "The API behind the site",
            "Everything",
            [new TechStackRef(2, "Old", null, "Backend")]));

        var stacks = new FakeTechStackRepository();
        stacks.TechStacks.Add(new TechStack(
            2, "C#", "https://example.test/csharp.svg", "Backend"));
        stacks.TechStacks.Add(new TechStack(5, "SQL", null, "Data"));

        return (projects, stacks);
    }

    private static void AssertFailure<T>(Result<T> result, ErrorKind kind, string code)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(kind, result.Error!.Kind);
        Assert.Equal(code, result.Error.Code);
    }
}
