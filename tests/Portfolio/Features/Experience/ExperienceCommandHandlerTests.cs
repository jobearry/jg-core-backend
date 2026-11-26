using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Application.Common;
using Portfolio.Domain.ValueObjects;
using DomainExperience = Portfolio.Domain.Entities.Experience;
using DomainProject = Portfolio.Domain.Entities.Project;

namespace Portfolio.Features.Experience.Tests;

/// <summary>
/// Admin write use cases for experience. The fakes keep the tests on the ports, and
/// the value-object invariants have to be rejected as validation, not as a 500.
/// </summary>
public class ExperienceCommandHandlerTests
{
    private static readonly DateTime StartedAt = new(2020, 1, 1);
    private static readonly DateTime FinishedAt = new(2021, 6, 30);

    [Fact]
    public async Task CreateExperience_WithValidRequest_CreatesAllScalarFields()
    {
        var experiences = new FakeExperienceRepository();
        var handler = new CreateExperienceHandler(
            experiences, NullLogger<CreateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateExperienceRequest(
                "Globex", "Staff Engineer", StartedAt, FinishedAt,
                "A role", "Owned the platform", "Corporate"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Id);
        Assert.Equal("Globex", result.Value.Company);
        Assert.Equal("Staff Engineer", result.Value.Role);
        Assert.Equal(StartedAt, result.Value.StartedAt);
        Assert.Equal(FinishedAt, result.Value.FinishedAt);
        Assert.Equal("A role", result.Value.Description);
        Assert.Equal("Owned the platform", result.Value.Responsibility);
        Assert.Equal("Corporate", result.Value.Type);
        Assert.Empty(result.Value.Projects);
    }

    [Fact]
    public async Task CreateExperience_WithBlankRole_LeavesItUndeclared()
    {
        var handler = new CreateExperienceHandler(
            new FakeExperienceRepository(), NullLogger<CreateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateExperienceRequest("Globex", "  ", StartedAt, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Role);
        Assert.Null(result.Value.FinishedAt);
    }

    [Fact]
    public async Task CreateExperience_WithNullBody_ReturnsValidationError()
    {
        var handler = new CreateExperienceHandler(
            new FakeExperienceRepository(), NullLogger<CreateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(null, CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateExperience_WithBlankCompany_ReturnsValidationError(string? company)
    {
        var handler = new CreateExperienceHandler(
            new FakeExperienceRepository(), NullLogger<CreateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateExperienceRequest(company!, null, StartedAt, null, null, null, null),
            CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task CreateExperience_WithTooLongCompany_ReturnsValidationError()
    {
        var handler = new CreateExperienceHandler(
            new FakeExperienceRepository(), NullLogger<CreateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            CompanyRequest(new string('X', Company.MaxLength + 1)), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task CreateExperience_WithTooLongRole_ReturnsValidationError()
    {
        var handler = new CreateExperienceHandler(
            new FakeExperienceRepository(), NullLogger<CreateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateExperienceRequest(
                "Globex", new string('R', Role.MaxLength + 1), StartedAt, null, null, null, null),
            CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task CreateExperience_WithFutureStart_ReturnsValidationError()
    {
        var handler = new CreateExperienceHandler(
            new FakeExperienceRepository(), NullLogger<CreateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateExperienceRequest(
                "Globex", null, DateTime.UtcNow.Date.AddDays(2), null, null, null, null),
            CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task CreateExperience_WithEndBeforeStart_ReturnsValidationError()
    {
        var handler = new CreateExperienceHandler(
            new FakeExperienceRepository(), NullLogger<CreateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateExperienceRequest(
                "Globex", null, FinishedAt, StartedAt, null, null, null),
            CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task UpdateExperience_ChangesFields_KeepsIdAndProjects()
    {
        var experiences = OneExperience();
        var handler = new UpdateExperienceHandler(
            experiences, NullLogger<UpdateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            4,
            new UpdateExperienceRequest(
                "Initech", "Staff Engineer", StartedAt, FinishedAt,
                "New description", "New responsibility", "Contract"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Id);
        Assert.Equal("Initech", result.Value.Company);
        Assert.Equal("New description", result.Value.Description);
        Assert.Equal("Contract", result.Value.Type);
        Assert.Equal("Portfolio API", Assert.Single(result.Value.Projects).ProjectName);
        Assert.Equal("Initech", Assert.Single(experiences.All).Company.Name);
    }

    [Fact]
    public async Task UpdateExperience_ForMissingId_ReturnsNotFound()
    {
        var handler = new UpdateExperienceHandler(
            new FakeExperienceRepository(), NullLogger<UpdateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            99, UpdateRequest(), CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateExperience_WithInvalidId_ReturnsValidationError(int id)
    {
        var handler = new UpdateExperienceHandler(
            new FakeExperienceRepository(), NullLogger<UpdateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(id, UpdateRequest(), CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task UpdateExperience_WithFutureStart_ReturnsValidationError()
    {
        var experiences = OneExperience();
        var handler = new UpdateExperienceHandler(
            experiences, NullLogger<UpdateExperienceHandler>.Instance);

        var result = await handler.HandleAsync(
            4,
            new UpdateExperienceRequest(
                "Globex", null, DateTime.UtcNow.Date.AddDays(2), null, null, null, null),
            CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
        Assert.Equal("Acme", Assert.Single(experiences.All).Company.Name);
    }

    [Fact]
    public async Task DeleteExperience_RemovesIt()
    {
        var experiences = OneExperience();
        var handler = new DeleteExperienceHandler(
            experiences, NullLogger<DeleteExperienceHandler>.Instance);

        var result = await handler.HandleAsync(4, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(experiences.All);
    }

    [Fact]
    public async Task DeleteExperience_ForMissingId_ReturnsNotFound()
    {
        var handler = new DeleteExperienceHandler(
            new FakeExperienceRepository(), NullLogger<DeleteExperienceHandler>.Instance);

        var result = await handler.HandleAsync(99, CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteExperience_WithInvalidId_ReturnsValidationError(int id)
    {
        var handler = new DeleteExperienceHandler(
            new FakeExperienceRepository(), NullLogger<DeleteExperienceHandler>.Instance);

        var result = await handler.HandleAsync(id, CancellationToken.None);

        AssertFailure(result, ErrorKind.Validation, "validation_failed");
    }

    [Fact]
    public async Task GetExperienceById_WhenFound_ReturnsDtoWithProjects()
    {
        var handler = new GetExperienceByIdHandler(
            OneExperience(), NullLogger<GetExperienceByIdHandler>.Instance);

        var result = await handler.HandleAsync(4, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Id);
        Assert.Equal("Acme", result.Value.Company);
        Assert.Equal("Portfolio API", Assert.Single(result.Value.Projects).ProjectName);
    }

    [Fact]
    public async Task GetExperienceById_WhenMissing_ReturnsNotFound()
    {
        var handler = new GetExperienceByIdHandler(
            new FakeExperienceRepository(), NullLogger<GetExperienceByIdHandler>.Instance);

        var result = await handler.HandleAsync(99, CancellationToken.None);

        AssertFailure(result, ErrorKind.NotFound, "not_found");
    }

    [Fact]
    public async Task GetExperienceById_WhenTheDatabaseFails_ReturnsInfrastructureError()
    {
        var handler = new GetExperienceByIdHandler(
            new FakeExperienceRepository { Failure = new FakeDbException("could not connect") },
            NullLogger<GetExperienceByIdHandler>.Instance);

        var result = await handler.HandleAsync(4, CancellationToken.None);

        AssertFailure(result, ErrorKind.Infrastructure, "database_unavailable");
    }

    private static CreateExperienceRequest CompanyRequest(string company) =>
        new(company, null, StartedAt, null, null, null, null);

    private static UpdateExperienceRequest UpdateRequest() =>
        new("Globex", null, StartedAt, null, null, null, null);

    private static FakeExperienceRepository OneExperience()
    {
        var experience = new DomainExperience(
            4, new Company("Acme"), new Period(StartedAt, null), new Role("Engineer"));
        experience.ReplaceProjects([new DomainProject(7, "Portfolio API")]);

        return new FakeExperienceRepository { All = { experience } };
    }

    private static void AssertFailure<T>(Result<T> result, ErrorKind kind, string code)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(kind, result.Error!.Kind);
        Assert.Equal(code, result.Error.Code);
    }
}
