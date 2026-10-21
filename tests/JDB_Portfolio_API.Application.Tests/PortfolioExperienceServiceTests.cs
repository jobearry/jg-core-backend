using JDB_Portfolio_API.Application.Services;
using JDB_Portfolio_API.Application.Tests.Fakes;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Tests;

public class PortfolioExperienceServiceTests
{
    [Fact]
    public async Task GetAllExperienceAsync_WhenRepositoryFails_ReturnsFailedResult()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            Failure = new InvalidOperationException("database is unreachable")
        };
        var service = new PortfolioExperienceService(repository);

        var result = await service.GetAllExperienceAsync(includeProjects: false);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.Error);
        Assert.Contains("database is unreachable", result.Error);
    }

    [Fact]
    public async Task GetAllExperienceAsync_WhenOperationIsCancelled_PropagatesCancellation()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            Failure = new OperationCanceledException()
        };
        var service = new PortfolioExperienceService(repository);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.GetAllExperienceAsync(includeProjects: false));
    }

    [Fact]
    public async Task GetAllExperienceAsync_WhenIncludeProjectsIsFalse_ReturnsPlainQueryResult()
    {
        var repository = new FakePortfolioExperienceRepository
        {
            All = new List<Experience> { AnExperience(1) },
            WithProjects = new List<Experience> { AnExperience(9) }
        };
        var service = new PortfolioExperienceService(repository);

        var result = await service.GetAllExperienceAsync(includeProjects: false);

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
        var service = new PortfolioExperienceService(repository);

        var result = await service.GetAllExperienceAsync(includeProjects: true);

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
        var service = new PortfolioExperienceService(repository);

        var result = await service.GetAllExperienceAsync(includeProjects: false);

        Assert.Equal(new[] { 3, 2, 1 }, result.Value!.Select(x => x.Id));
    }

    private static Experience AnExperience(int id) => new(
        id,
        new Company("Acme"),
        new Period(DateTime.UtcNow.AddYears(-1), null),
        new Role("Developer"));
}
