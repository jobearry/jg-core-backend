using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Application.Common;
using Portfolio.Domain.ValueObjects;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Project.Tests;

/// <summary>
/// Admin write use cases for projects. Fakes keep the tests on the ports: the
/// experience link and tech-stack refs must survive an edit untouched.
/// </summary>
public class ProjectCommandHandlerTests
{
    private static readonly TechStackRef[] StackRefs =
        [new(2, "C#", "https://example.test/csharp.svg", "Backend")];

    [Fact]
    public async Task CreateProject_WithValidRequest_CreatesFieldsOnly()
    {
        var projects = new FakeProjectRepository();
        var handler = new CreateProjectHandler(
            projects, NullLogger<CreateProjectHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateProjectRequest("Portfolio API", "A backend", "Everything"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Portfolio API", result.Value!.ProjectName);
        var created = Assert.Single(projects.Projects);
        Assert.Equal("A backend", created.Description);
        Assert.Equal("Everything", created.Contribution);
        Assert.Null(created.ExperienceId);
        Assert.Empty(created.TechStacks);
    }

    [Fact]
    public async Task CreateProject_WithBlankName_ReturnsValidationError()
    {
        var projects = new FakeProjectRepository();
        var handler = new CreateProjectHandler(
            projects, NullLogger<CreateProjectHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateProjectRequest("   ", null, null), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Empty(projects.Projects);
    }

    [Fact]
    public async Task CreateProject_WithNullBody_ReturnsValidationError()
    {
        var handler = new CreateProjectHandler(
            new FakeProjectRepository(), NullLogger<CreateProjectHandler>.Instance);

        var result = await handler.HandleAsync(null, CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task UpdateProject_ChangesFields_KeepsIdExperienceAndStacks()
    {
        var projects = ProjectsWithOne();
        var handler = new UpdateProjectHandler(
            projects, NullLogger<UpdateProjectHandler>.Instance);

        var result = await handler.HandleAsync(
            7,
            new UpdateProjectRequest("Portfolio API v2", "New description", "New contribution"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value!.Id);
        Assert.Equal("Portfolio API v2", result.Value.ProjectName);
        Assert.Equal(4, Assert.Single(projects.Projects).ExperienceId);
        var stack = Assert.Single(result.Value.TechStacks);
        Assert.Equal(2, stack.Id);
        Assert.Equal("C#", stack.StackName);
        Assert.Equal("Backend", stack.Category);
    }

    [Fact]
    public async Task UpdateProject_ForMissingId_ReturnsNotFound()
    {
        var handler = new UpdateProjectHandler(
            new FakeProjectRepository(), NullLogger<UpdateProjectHandler>.Instance);

        var result = await handler.HandleAsync(
            99, new UpdateProjectRequest("Name", null, null), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateProject_WithInvalidId_ReturnsValidationError(int id)
    {
        var handler = new UpdateProjectHandler(
            new FakeProjectRepository(), NullLogger<UpdateProjectHandler>.Instance);

        var result = await handler.HandleAsync(
            id, new UpdateProjectRequest("Name", null, null), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task UpdateProject_WithBlankName_ReturnsValidationError()
    {
        var projects = ProjectsWithOne();
        var handler = new UpdateProjectHandler(
            projects, NullLogger<UpdateProjectHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new UpdateProjectRequest("", null, null), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Equal("Portfolio API", Assert.Single(projects.Projects).ProjectName);
    }

    [Fact]
    public async Task DeleteProject_RemovesIt()
    {
        var projects = ProjectsWithOne();
        var handler = new DeleteProjectHandler(
            projects, NullLogger<DeleteProjectHandler>.Instance);

        var result = await handler.HandleAsync(7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(projects.Projects);
    }

    [Fact]
    public async Task DeleteProject_ForMissingId_ReturnsNotFound()
    {
        var handler = new DeleteProjectHandler(
            new FakeProjectRepository(), NullLogger<DeleteProjectHandler>.Instance);

        var result = await handler.HandleAsync(99, CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteProject_WithInvalidId_ReturnsValidationError(int id)
    {
        var handler = new DeleteProjectHandler(
            new FakeProjectRepository(), NullLogger<DeleteProjectHandler>.Instance);

        var result = await handler.HandleAsync(id, CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task GetProjectById_WhenFound_ReturnsTheDtoWithStackRefs()
    {
        var handler = new GetProjectByIdHandler(
            ProjectsWithOne(), NullLogger<GetProjectByIdHandler>.Instance);

        var result = await handler.HandleAsync(7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value!.Id);
        var stack = Assert.Single(result.Value.TechStacks);
        Assert.Equal(2, stack.Id);
        Assert.Equal("C#", stack.StackName);
    }

    [Fact]
    public async Task GetProjectById_WhenMissing_ReturnsNotFound()
    {
        var handler = new GetProjectByIdHandler(
            new FakeProjectRepository(), NullLogger<GetProjectByIdHandler>.Instance);

        var result = await handler.HandleAsync(99, CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task GetProjectById_WhenTheDatabaseFails_ReturnsInfrastructureError()
    {
        var projects = new FakeProjectRepository
        {
            Failure = new FakeDbException("could not connect")
        };
        var handler = new GetProjectByIdHandler(
            projects, NullLogger<GetProjectByIdHandler>.Instance);

        var result = await handler.HandleAsync(7, CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
    }

    private static FakeProjectRepository ProjectsWithOne() => new()
    {
        Projects =
        {
            new DomainProject(7, 4, "Portfolio API", "A backend", "Everything", StackRefs)
        }
    };

    private static void AssertFailure<T>(Result<T> result, ErrorKind kind, string code)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(kind, result.Error!.Kind);
        Assert.Equal(code, result.Error.Code);
    }
}
