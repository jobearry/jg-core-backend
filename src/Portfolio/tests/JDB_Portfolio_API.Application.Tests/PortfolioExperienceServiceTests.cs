using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.Services;
using JDB_Portfolio_API.Application.Tests.Fakes;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Tests;

public class PortfolioExperienceServiceTests
{
    private const string Operation = "get portfolio experience content";

    /// <summary>
    /// A database failure must be typed, not stringly, and must not leak the provider's
    /// message: Npgsql puts the host, the database and sometimes the SQL in there.
    /// </summary>
    [Fact]
    public async Task GetAllExperienceAsync_WhenTheDatabaseFails_ReturnsInfrastructureError()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            Failure = new FakeDbException("could not connect to host 10.0.0.5 as user jg")
        };
        var service = Service(repository);

        var result = await service.GetAllExperienceAsync(includeProjects: false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Infrastructure, result.Error!.Kind);
        Assert.Equal("database_unavailable", result.Error.Code);

        Assert.DoesNotContain("10.0.0.5", result.Error.Message);
        Assert.DoesNotContain("could not connect", result.Error.Message);
    }

    /// <summary>
    /// The detail that used to be published to the client has to go somewhere, or the
    /// failure is undiagnosable. It goes to the log, together with the exception.
    /// </summary>
    [Fact]
    public async Task GetAllExperienceAsync_WhenTheDatabaseFails_LogsAWarningWithTheOriginalException()
    {
        var exception = new FakeDbException("could not connect to host 10.0.0.5");
        var repository = new FakePortfolioExperienceRepository { Failure = exception };
        var logger = new RecordingLogger<PortfolioExperienceService>();
        var service = new PortfolioExperienceService(repository, logger);

        await service.GetAllExperienceAsync(includeProjects: false, CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Contains(Operation, entry.Message);
        Assert.Contains("database_unavailable", entry.Message);

        // The host name is still available for diagnosis — in the log, not on the wire.
        Assert.Contains("10.0.0.5", entry.Exception!.Message);
    }

    /// <summary>
    /// A bug is not an outage. If this were classified as infrastructure the endpoint
    /// would answer 503, teaching clients to retry something that will never succeed.
    /// </summary>
    [Fact]
    public async Task GetAllExperienceAsync_WhenSomethingUnexpectedThrows_ReturnsUnexpectedError()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            Failure = new NullReferenceException("Object reference not set")
        };
        var service = Service(repository);

        var result = await service.GetAllExperienceAsync(includeProjects: false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unexpected, result.Error!.Kind);
        Assert.Equal("unexpected_error", result.Error.Code);
    }

    /// <summary>
    /// Only an unexpected exception is logged at Error. A dependency outage is a
    /// warning: it is expected, it is already visible to the caller as a 503, and it
    /// must not drown out the failures that need someone to fix them.
    /// </summary>
    [Fact]
    public async Task GetAllExperienceAsync_WhenSomethingUnexpectedThrows_LogsAnError()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            Failure = new NullReferenceException("Object reference not set")
        };
        var logger = new RecordingLogger<PortfolioExperienceService>();
        var service = new PortfolioExperienceService(repository, logger);

        await service.GetAllExperienceAsync(includeProjects: false, CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
    }

    [Fact]
    public async Task GetAllExperienceAsync_WhenAnHttpCallFails_ReturnsUpstreamError()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            Failure = new HttpRequestException("no route to host")
        };
        var service = Service(repository);

        var result = await service.GetAllExperienceAsync(includeProjects: false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Upstream, result.Error!.Kind);
    }

    [Fact]
    public async Task GetAllExperienceAsync_WhenOperationIsCancelled_PropagatesCancellation()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            Failure = new OperationCanceledException()
        };
        var service = Service(repository);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.GetAllExperienceAsync(includeProjects: false, CancellationToken.None));
    }

    /// <summary>
    /// Plan 2.4. The token has to arrive at the port, or the database query keeps
    /// running after the client has gone. Asserting on the exact token is the only way
    /// to prove it was threaded rather than replaced with a fresh <c>None</c>.
    /// </summary>
    [Fact]
    public async Task GetAllExperienceAsync_PassesTheCallersCancellationTokenToTheRepository()
    {
        var repository = new FakePortfolioExperienceRepository();
        var service = Service(repository);
        using var cancellation = new CancellationTokenSource();

        await service.GetAllExperienceAsync(includeProjects: true, cancellation.Token);

        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
    }

    [Fact]
    public async Task GetAllExperienceAsync_WhenIncludeProjectsIsFalse_ReturnsPlainQueryResult()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            All = new List<Experience> { AnExperience(1) },
            WithProjects = new List<Experience> { AnExperience(9) }
        };
        var service = Service(repository);

        var result = await service.GetAllExperienceAsync(includeProjects: false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1 }, result.Value!.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAllExperienceAsync_WhenIncludeProjectsIsTrue_ReturnsProjectsQueryResult()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            All = new List<Experience> { AnExperience(1) },
            WithProjects = new List<Experience> { AnExperience(9) }
        };
        var service = Service(repository);

        var result = await service.GetAllExperienceAsync(includeProjects: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 9 }, result.Value!.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAllExperienceAsync_ReturnsExperiencesOrderedByIdDescending()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            All = new List<Experience> { AnExperience(1), AnExperience(3), AnExperience(2) }
        };
        var service = Service(repository);

        var result = await service.GetAllExperienceAsync(includeProjects: false, CancellationToken.None);

        Assert.Equal(new[] { 3, 2, 1 }, result.Value!.Select(x => x.Id));
    }

    private static PortfolioExperienceService Service(FakePortfolioExperienceRepository repository) =>
        new(repository, NullLogger<PortfolioExperienceService>.Instance);

    private static Experience AnExperience(int id) => new(
        id,
        new Company("Acme"),
        new Period(DateTime.UtcNow.AddYears(-1), null),
        new Role("Developer"));

    /// <summary>
    /// <see cref="DbException"/> has protected constructors, so a test cannot instantiate
    /// it directly. Subclassing is the only way to simulate a provider failure without
    /// taking a dependency on Npgsql in the Application tests.
    /// </summary>
    private sealed class FakeDbException : DbException
    {
        public FakeDbException(string message) : base(message) { }
    }
}
