using Portfolio.Api.Tests.Fakes;
using Portfolio.Application.Common;
using Portfolio.Domain.ValueObjects;
using Portfolio.Features.Experience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience.Tests;

public class GetExperienceHandlerTests
{
    private const string Operation = "get portfolio experience content";

    /// <summary>
    /// A database failure must be typed, not stringly, and must not leak the provider's
    /// message: Npgsql puts the host, the database and sometimes the SQL in there.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenTheDatabaseFails_ReturnsInfrastructureError()
    {
        var repository = new FakeExperienceRepository
        {
            Failure = new FakeDbException("could not connect to host 10.0.0.5 as user jg")
        };
        var handler = Service(repository);

        var result = await handler.HandleAsync(includeProjects: false, CancellationToken.None);

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
    public async Task HandleAsync_WhenTheDatabaseFails_LogsAWarningWithTheOriginalException()
    {
        var exception = new FakeDbException("could not connect to host 10.0.0.5");
        var repository = new FakeExperienceRepository { Failure = exception };
        var logger = new RecordingLogger<GetExperienceHandler>();
        var handler = new GetExperienceHandler(repository, logger);

        await handler.HandleAsync(includeProjects: false, CancellationToken.None);

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
    public async Task HandleAsync_WhenSomethingUnexpectedThrows_ReturnsUnexpectedError()
    {
        var repository = new FakeExperienceRepository
        {
            Failure = new NullReferenceException("Object reference not set")
        };
        var handler = Service(repository);

        var result = await handler.HandleAsync(includeProjects: false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Unexpected, result.Error!.Kind);
        Assert.Equal("unexpected_error", result.Error.Code);
    }

    /// <summary>
    /// Only an unexpected exception is logged at Error. A dependency outage is a
    /// warning: it is expected to happen and is already visible to the caller as a 503.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenSomethingUnexpectedThrows_LogsAnError()
    {
        var repository = new FakeExperienceRepository
        {
            Failure = new NullReferenceException("Object reference not set")
        };
        var logger = new RecordingLogger<GetExperienceHandler>();
        var handler = new GetExperienceHandler(repository, logger);

        await handler.HandleAsync(includeProjects: false, CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
    }

    [Fact]
    public async Task HandleAsync_WhenAnHttpCallFails_ReturnsUpstreamError()
    {
        var repository = new FakeExperienceRepository
        {
            Failure = new HttpRequestException("no route to host")
        };
        var handler = Service(repository);

        var result = await handler.HandleAsync(includeProjects: false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Upstream, result.Error!.Kind);
    }

    [Fact]
    public async Task HandleAsync_WhenOperationIsCancelled_PropagatesCancellation()
    {
        var repository = new FakeExperienceRepository
        {
            Failure = new OperationCanceledException()
        };
        var handler = Service(repository);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.HandleAsync(includeProjects: false, CancellationToken.None));
    }

    /// <summary>
    /// Plan 2.4. The token has to arrive at the port, or the database query keeps
    /// running after the client has gone. Asserting on the exact token is the only way
    /// to prove it was threaded rather than replaced with a fresh <c>None</c>.
    /// </summary>
    [Fact]
    public async Task HandleAsync_PassesTheCallersCancellationTokenToTheRepository()
    {
        var repository = new FakeExperienceRepository();
        var handler = Service(repository);
        using var cancellation = new CancellationTokenSource();

        await handler.HandleAsync(includeProjects: true, cancellation.Token);

        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeProjectsIsFalse_ReturnsPlainQueryResult()
    {
        var repository = new FakeExperienceRepository
        {
            All = new List<DomainExperience> { AnExperience(1) },
            WithProjects = new List<DomainExperience> { AnExperience(9) }
        };
        var handler = Service(repository);

        var result = await handler.HandleAsync(includeProjects: false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1 }, result.Value!.Select(x => x.Id));
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeProjectsIsTrue_ReturnsProjectsQueryResult()
    {
        var repository = new FakeExperienceRepository
        {
            All = new List<DomainExperience> { AnExperience(1) },
            WithProjects = new List<DomainExperience> { AnExperience(9) }
        };
        var handler = Service(repository);

        var result = await handler.HandleAsync(includeProjects: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 9 }, result.Value!.Select(x => x.Id));
    }

    [Fact]
    public async Task HandleAsync_ReturnsExperiencesOrderedByIdDescending()
    {
        var repository = new FakeExperienceRepository
        {
            All = new List<DomainExperience> { AnExperience(1), AnExperience(3), AnExperience(2) }
        };
        var handler = Service(repository);

        var result = await handler.HandleAsync(includeProjects: false, CancellationToken.None);

        Assert.Equal(new[] { 3, 2, 1 }, result.Value!.Select(x => x.Id));
    }

    private static GetExperienceHandler Service(FakeExperienceRepository repository) =>
        new(repository, NullLogger<GetExperienceHandler>.Instance);

    private static DomainExperience AnExperience(int id) => new(
        id,
        new Company("Acme"),
        new Period(DateTime.UtcNow.AddYears(-1), null),
        new Role("Developer"));
}
