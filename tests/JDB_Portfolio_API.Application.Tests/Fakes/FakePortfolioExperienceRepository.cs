using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Tests.Fakes;

/// <summary>
/// Hand-rolled fake for the experience repository port. It returns canned data and
/// can be told to fail, so a test never needs a database.
/// </summary>
internal class FakePortfolioExperienceRepository : IPortfolioExperienceRepository
{
    /// <summary>Data returned by <see cref="GetAllAsync"/>.</summary>
    public List<Experience> All { get; set; } = new();

    /// <summary>Data returned by <see cref="GetAllExperienceWithProjectsAsync"/>.</summary>
    public List<Experience> WithProjects { get; set; } = new();

    /// <summary>When set, every call fails with this exception.</summary>
    public Exception? Failure { get; set; }

    /// <summary>
    /// The token the last call received. Used to prove the token is threaded from the
    /// service to the port instead of being quietly dropped.
    /// </summary>
    public CancellationToken LastCancellationToken { get; private set; }

    public Task<List<Experience>> GetAllAsync(CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;

        return Failure is null
            ? Task.FromResult(All)
            : Task.FromException<List<Experience>>(Failure);
    }

    public Task<List<Experience>> GetAllExperienceWithProjectsAsync(CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;

        return Failure is null
            ? Task.FromResult(WithProjects)
            : Task.FromException<List<Experience>>(Failure);
    }
}
