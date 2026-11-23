using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainExperience = Portfolio.Domain.Entities.Experience;

namespace Portfolio.Features.Experience.Tests;

/// <summary>
/// Hand-rolled fake for the experience repository port. It returns canned data and
/// can be told to fail, so a test never needs a database.
/// </summary>
internal sealed class FakeExperienceRepository : IExperienceRepository
{
    /// <summary>Data returned by <see cref="GetAllAsync"/>.</summary>
    public List<DomainExperience> All { get; set; } = new();

    /// <summary>Data returned by <see cref="GetAllWithProjectsAsync"/>.</summary>
    public List<DomainExperience> WithProjects { get; set; } = new();

    /// <summary>When set, every call fails with this exception.</summary>
    public Exception? Failure { get; set; }

    /// <summary>
    /// The token the last call received. Used to prove the token is threaded from the
    /// handler to the port instead of being quietly dropped.
    /// </summary>
    public CancellationToken LastCancellationToken { get; private set; }

    public Task<List<DomainExperience>> GetAllAsync(CancellationToken cancellationToken) =>
        Respond(All, cancellationToken);

    public Task<List<DomainExperience>> GetAllWithProjectsAsync(CancellationToken cancellationToken) =>
        Respond(WithProjects, cancellationToken);

    private Task<List<DomainExperience>> Respond(
        List<DomainExperience> data,
        CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;

        return Failure is null
            ? Task.FromResult(data)
            : Task.FromException<List<DomainExperience>>(Failure);
    }
}
