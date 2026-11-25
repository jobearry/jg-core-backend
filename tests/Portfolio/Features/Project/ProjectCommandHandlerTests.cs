using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Application.Common;
using Portfolio.Domain.ValueObjects;
using DomainExperience = Portfolio.Domain.Entities.Experience;
using DomainProject = Portfolio.Domain.Entities.Project;
using UpdateProjectExperienceRequest = Portfolio.Features.Project.UpdateProjectExperienceRequest;

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
    public async Task CreateProjectForExperience_WithValidRequest_AttachesTheProject()
    {
        var projects = new FakeProjectRepository();
        var experiences = new FakeExperienceRepository
        {
            Experiences = { AnExperience(4) }
        };
        var handler = new CreateProjectForExperienceHandler(
            projects, experiences, NullLogger<CreateProjectForExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            4,
            new CreateProjectRequest("Portfolio API", "A backend", "Everything"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Portfolio API", result.Value!.ProjectName);
        Assert.Equal(4, Assert.Single(projects.Projects).ExperienceId);
    }

    [Fact]
    public async Task CreateProjectForExperience_ForMissingExperience_ReturnsNotFoundAndWritesNothing()
    {
        var projects = new FakeProjectRepository();
        var handler = new CreateProjectForExperienceHandler(
            projects, new FakeExperienceRepository(),
            NullLogger<CreateProjectForExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            99, new CreateProjectRequest("Portfolio API", null, null), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
        Assert.Empty(projects.Projects);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateProjectForExperience_WithInvalidExperienceId_ReturnsValidationError(int id)
    {
        var projects = new FakeProjectRepository();
        var handler = new CreateProjectForExperienceHandler(
            projects, new FakeExperienceRepository(),
            NullLogger<CreateProjectForExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            id, new CreateProjectRequest("Portfolio API", null, null), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Empty(projects.Projects);
    }

    [Fact]
    public async Task CreateProjectForExperience_WithNullBody_ReturnsValidationError()
    {
        var handler = new CreateProjectForExperienceHandler(
            new FakeProjectRepository(), new FakeExperienceRepository(),
            NullLogger<CreateProjectForExperienceHandler>.Instance);

        var result = await handler.HandleAsync(4, null, CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task CreateProjectForExperience_WithBlankName_ReturnsValidationError()
    {
        var projects = new FakeProjectRepository();
        var handler = new CreateProjectForExperienceHandler(
            projects, new FakeExperienceRepository(),
            NullLogger<CreateProjectForExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            4, new CreateProjectRequest("  ", null, null), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Empty(projects.Projects);
    }

    [Fact]
    public async Task CreateProjectForExperience_WhenExperienceLookupFails_ReturnsInfrastructureError()
    {
        var projects = new FakeProjectRepository();
        var experiences = new FakeExperienceRepository
        {
            Failure = new FakeDbException("database down")
        };
        var handler = new CreateProjectForExperienceHandler(
            projects, experiences, NullLogger<CreateProjectForExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            4, new CreateProjectRequest("Portfolio API", null, null), CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
        Assert.Empty(projects.Projects);
    }

    [Fact]
    public async Task UpdateProjectExperience_Attach_AttachesProjectToExperience()
    {
        var projects = new FakeProjectRepository
        {
            Projects = { new DomainProject(7, null, "Project A", "Desc", "Contr", Array.Empty<TechStackRef>()) }
        };
        var experiences = new FakeExperienceRepository
        {
            Experiences = { AnExperience(4) }
        };
        var handler = new UpdateProjectExperienceHandler(
            projects, experiences, NullLogger<UpdateProjectExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            7,
            new UpdateProjectExperienceRequest(4),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, Assert.Single(projects.Projects).ExperienceId);
    }

    [Fact]
    public async Task UpdateProjectExperience_Move_ChangesExperience()
    {
        var projects = new FakeProjectRepository
        {
            Projects = { new DomainProject(7, 4, "Project A", "Desc", "Contr", Array.Empty<TechStackRef>()) }
        };
        var experiences = new FakeExperienceRepository
        {
            Experiences = { AnExperience(4), AnExperience(9) }
        };
        var handler = new UpdateProjectExperienceHandler(
            projects, experiences, NullLogger<UpdateProjectExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            7,
            new UpdateProjectExperienceRequest(9),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(9, Assert.Single(projects.Projects).ExperienceId);
    }

    [Fact]
    public async Task UpdateProjectExperience_Detach_SetsExperienceIdNull()
    {
        var projects = new FakeProjectRepository
        {
            Projects = { new DomainProject(7, 4, "Project A", "Desc", "Contr", Array.Empty<TechStackRef>()) }
        };
        var handler = new UpdateProjectExperienceHandler(
            projects, new FakeExperienceRepository(),
            NullLogger<UpdateProjectExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            7,
            new UpdateProjectExperienceRequest(null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(projects.Projects).ExperienceId);
    }

    [Fact]
    public async Task UpdateProjectExperience_ForMissingProject_ReturnsNotFound()
    {
        var projects = new FakeProjectRepository();
        var handler = new UpdateProjectExperienceHandler(
            projects, new FakeExperienceRepository(),
            NullLogger<UpdateProjectExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            99, new UpdateProjectExperienceRequest(4), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateProjectExperience_WithInvalidProjectId_ReturnsValidationError(int id)
    {
        var handler = new UpdateProjectExperienceHandler(
            new FakeProjectRepository(), new FakeExperienceRepository(),
            NullLogger<UpdateProjectExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            id, new UpdateProjectExperienceRequest(4), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateProjectExperience_WithInvalidExperienceId_ReturnsValidationError(int experienceId)
    {
        var projects = new FakeProjectRepository
        {
            Projects = { new DomainProject(7, null, "Project A", "Desc", "Contr", Array.Empty<TechStackRef>()) }
        };
        var handler = new UpdateProjectExperienceHandler(
            projects, new FakeExperienceRepository(),
            NullLogger<UpdateProjectExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new UpdateProjectExperienceRequest(experienceId), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task UpdateProjectExperience_ForMissingExperience_ReturnsNotFound()
    {
        var projects = new FakeProjectRepository
        {
            Projects = { new DomainProject(7, null, "Project A", "Desc", "Contr", Array.Empty<TechStackRef>()) }
        };
        var handler = new UpdateProjectExperienceHandler(
            projects, new FakeExperienceRepository(),
            NullLogger<UpdateProjectExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new UpdateProjectExperienceRequest(99), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task UpdateProjectExperience_WithNullBody_ReturnsValidationError()
    {
        var handler = new UpdateProjectExperienceHandler(
            new FakeProjectRepository(), new FakeExperienceRepository(),
            NullLogger<UpdateProjectExperienceHandler>.Instance);

        var result = await handler.HandleAsync(7, null, CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task UpdateProjectExperience_WhenExperienceLookupFails_ReturnsInfrastructureError()
    {
        var projects = new FakeProjectRepository
        {
            Projects = { new DomainProject(7, null, "Project A", "Desc", "Contr", Array.Empty<TechStackRef>()) }
        };
        var experiences = new FakeExperienceRepository
        {
            Failure = new FakeDbException("database down")
        };
        var handler = new UpdateProjectExperienceHandler(
            projects, experiences, NullLogger<UpdateProjectExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new UpdateProjectExperienceRequest(4), CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
    }

    [Fact]
    public async Task UpdateProjectExperience_WhenProjectUpdateFails_ReturnsInfrastructureError()
    {
        var projects = new FakeProjectRepository
        {
            Projects = { new DomainProject(7, null, "Project A", "Desc", "Contr", Array.Empty<TechStackRef>()) },
            Failure = new FakeDbException("database down")
        };
        var experiences = new FakeExperienceRepository
        {
            Experiences = { AnExperience(4) }
        };
        var handler = new UpdateProjectExperienceHandler(
            projects, experiences, NullLogger<UpdateProjectExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            7, new UpdateProjectExperienceRequest(4), CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
    }

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

    private static DomainExperience AnExperience(int id) =>
        new(
            id,
            new Company("Applied ICT Inc."),
            new Period(new DateTime(2020, 1, 1), null),
            new Role("Software Engineer"));

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
