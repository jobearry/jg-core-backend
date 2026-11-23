using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Api.Tests.Fakes;
using Portfolio.Application.Common;
using Portfolio.Domain.ValueObjects;
using Portfolio.Features.Recommendation;
using DomainRecommendation = Portfolio.Domain.Entities.Recommendation;

namespace Portfolio.Features.Recommendation.Tests;

/// <summary>
/// Admin write use cases. The fake repository stands in for the EF adapter so these
/// tests lock validation, not-found and timestamp rules without touching PostgreSQL.
/// </summary>
public class RecommendationCommandHandlerTests
{
    private static readonly DateTime OriginalPostedAt =
        new(2026, 6, 27, 9, 30, 0, DateTimeKind.Utc);

    private static readonly DateTimeOffset ServerNow =
        new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetByIdAsync_WhenTheRecommendationExists_ReturnsItsDto()
    {
        var repository = new FakeRecommendationRepository();
        repository.Recommendations.Add(new DomainRecommendation(
            1, "Jane Doe", "Excellent teammate", new Role("Engineer"), OriginalPostedAt));
        var handler = new GetRecommendationByIdHandler(
            repository, NullLogger<GetRecommendationByIdHandler>.Instance);

        var result = await handler.HandleAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Id);
        Assert.Equal("Jane Doe", result.Value.PersonName);
        Assert.Equal("Engineer", result.Value.CareerPosition);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTheRecommendationIsMissing_ReturnsNotFoundError()
    {
        var handler = new GetRecommendationByIdHandler(
            new FakeRecommendationRepository(), NullLogger<GetRecommendationByIdHandler>.Instance);

        var result = await handler.HandleAsync(99, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsValidationError()
    {
        var handler = new GetRecommendationByIdHandler(
            new FakeRecommendationRepository(), NullLogger<GetRecommendationByIdHandler>.Instance);

        var result = await handler.HandleAsync(0, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_UsesServerTimeAndReturnsCreatedDto()
    {
        var repository = new FakeRecommendationRepository();
        var handler = new CreateRecommendationHandler(
            repository,
            NullLogger<CreateRecommendationHandler>.Instance,
            new FixedTimeProvider(ServerNow));

        var result = await handler.HandleAsync(
            new CreateRecommendationRequest("Jane Doe", "Excellent teammate", "Staff Engineer"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane Doe", result.Value!.PersonName);
        Assert.Equal("Excellent teammate", result.Value.Description);
        Assert.Equal("Staff Engineer", result.Value.CareerPosition);
        Assert.Equal(ServerNow.UtcDateTime, result.Value.PostedAt);
        Assert.Single(repository.Recommendations);
    }

    [Fact]
    public async Task CreateAsync_WithBlankPersonName_ReturnsValidationError()
    {
        var repository = new FakeRecommendationRepository();
        var handler = new CreateRecommendationHandler(
            repository,
            NullLogger<CreateRecommendationHandler>.Instance,
            new FixedTimeProvider(ServerNow));

        var result = await handler.HandleAsync(
            new CreateRecommendationRequest("   ", "Excellent teammate", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("validation_failed", result.Error.Code);
        Assert.Empty(repository.Recommendations);
    }

    [Fact]
    public async Task CreateAsync_WithOverlongPersonName_ReturnsValidationError()
    {
        var repository = new FakeRecommendationRepository();
        var handler = new CreateRecommendationHandler(
            repository,
            NullLogger<CreateRecommendationHandler>.Instance,
            new FixedTimeProvider(ServerNow));

        var result = await handler.HandleAsync(
            new CreateRecommendationRequest(new string('a', 51), "Excellent teammate", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("validation_failed", result.Error.Code);
        Assert.Empty(repository.Recommendations);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithBlankCareerPosition_ReturnsValidationError(string careerPosition)
    {
        var repository = new FakeRecommendationRepository();
        var handler = new CreateRecommendationHandler(
            repository,
            NullLogger<CreateRecommendationHandler>.Instance,
            new FixedTimeProvider(ServerNow));

        var result = await handler.HandleAsync(
            new CreateRecommendationRequest("Jane Doe", "Excellent teammate", careerPosition),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("validation_failed", result.Error.Code);
        Assert.Empty(repository.Recommendations);
    }

    [Fact]
    public async Task CreateAsync_WithOverlongCareerPosition_ReturnsValidationError()
    {
        var repository = new FakeRecommendationRepository();
        var handler = new CreateRecommendationHandler(
            repository,
            NullLogger<CreateRecommendationHandler>.Instance,
            new FixedTimeProvider(ServerNow));

        var result = await handler.HandleAsync(
            new CreateRecommendationRequest(
                "Jane Doe", "Excellent teammate", new string('a', Role.MaxLength + 1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("validation_failed", result.Error.Code);
        Assert.Empty(repository.Recommendations);
    }

    [Fact]
    public async Task CreateAsync_WithNullRequest_ReturnsValidationError()
    {
        var handler = new CreateRecommendationHandler(
            new FakeRecommendationRepository(),
            NullLogger<CreateRecommendationHandler>.Instance,
            new FixedTimeProvider(ServerNow));

        var result = await handler.HandleAsync(null!, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenTheDatabaseFails_ReturnsInfrastructureError()
    {
        var repository = new FakeRecommendationRepository
        {
            Failure = new FakeDbException("connection refused")
        };
        var handler = new CreateRecommendationHandler(
            repository,
            NullLogger<CreateRecommendationHandler>.Instance,
            new FixedTimeProvider(ServerNow));

        var result = await handler.HandleAsync(
            new CreateRecommendationRequest("Jane Doe", "Excellent teammate", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Infrastructure, result.Error!.Kind);
        Assert.Equal("database_unavailable", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateAsync_WithInvalidId_ReturnsValidationError(int id)
    {
        var handler = new UpdateRecommendationHandler(
            new FakeRecommendationRepository(),
            NullLogger<UpdateRecommendationHandler>.Instance);

        var result = await handler.HandleAsync(
            id,
            new UpdateRecommendationRequest("Jane Doe", "Excellent teammate", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WhenTheRecommendationIsMissing_ReturnsNotFoundError()
    {
        var handler = new UpdateRecommendationHandler(
            new FakeRecommendationRepository(),
            NullLogger<UpdateRecommendationHandler>.Instance);

        var result = await handler.HandleAsync(
            99,
            new UpdateRecommendationRequest("Jane Doe", "Excellent teammate", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ChangesEditableFieldsButNotPostedAt()
    {
        var repository = new FakeRecommendationRepository();
        repository.Recommendations.Add(new DomainRecommendation(
            1, "Old Name", "Old description", new Role("Engineer"), OriginalPostedAt));
        var handler = new UpdateRecommendationHandler(
            repository,
            NullLogger<UpdateRecommendationHandler>.Instance);

        var result = await handler.HandleAsync(
            1,
            new UpdateRecommendationRequest("New Name", "New description", "Manager"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value!.PersonName);
        Assert.Equal("New description", result.Value.Description);
        Assert.Equal("Manager", result.Value.CareerPosition);
        Assert.Equal(OriginalPostedAt, result.Value.PostedAt);
        Assert.Equal(OriginalPostedAt, Assert.Single(repository.Recommendations).PostedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithNullRequest_ReturnsValidationError()
    {
        var handler = new UpdateRecommendationHandler(
            new FakeRecommendationRepository(),
            NullLogger<UpdateRecommendationHandler>.Instance);

        var result = await handler.HandleAsync(1, null!, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("validation_failed", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteAsync_WithInvalidId_ReturnsValidationError(int id)
    {
        var handler = new DeleteRecommendationHandler(
            new FakeRecommendationRepository(),
            NullLogger<DeleteRecommendationHandler>.Instance);

        var result = await handler.HandleAsync(id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenTheRecommendationIsMissing_ReturnsNotFoundError()
    {
        var handler = new DeleteRecommendationHandler(
            new FakeRecommendationRepository(),
            NullLogger<DeleteRecommendationHandler>.Instance);

        var result = await handler.HandleAsync(99, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenTheRecommendationExists_RemovesIt()
    {
        var repository = new FakeRecommendationRepository();
        repository.Recommendations.Add(new DomainRecommendation(
            1, "Jane Doe", "Excellent teammate", null, OriginalPostedAt));
        var handler = new DeleteRecommendationHandler(
            repository,
            NullLogger<DeleteRecommendationHandler>.Instance);

        var result = await handler.HandleAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.Recommendations);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
