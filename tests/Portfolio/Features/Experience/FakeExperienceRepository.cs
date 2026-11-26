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

    public Task<DomainExperience?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;

        return Failure is not null
            ? Task.FromException<DomainExperience?>(Failure)
            : Task.FromResult(All.FirstOrDefault(experience => experience.Id == id));
    }

    public Task<DomainExperience> AddAsync(
        DomainExperience experience, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (Failure is not null)
        {
            return Task.FromException<DomainExperience>(Failure);
        }

        var created = WithId(
            All.Count == 0 ? 1 : All.Max(item => item.Id) + 1, experience);
        All.Add(created);
        return Task.FromResult(created);
    }

    public Task<DomainExperience> UpdateAsync(
        DomainExperience experience, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (Failure is not null)
        {
            return Task.FromException<DomainExperience>(Failure);
        }

        var index = All.FindIndex(item => item.Id == experience.Id);
        if (index >= 0)
        {
            All[index] = WithId(experience.Id, experience);
        }

        return Task.FromResult(experience);
    }

    public Task DeleteAsync(DomainExperience experience, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (Failure is not null)
        {
            return Task.FromException(Failure);
        }

        All.RemoveAll(item => item.Id == experience.Id);
        return Task.CompletedTask;
    }

    private static DomainExperience WithId(int id, DomainExperience source)
    {
        var copy = new DomainExperience(id, source.Company, source.Duration, source.Role);
        copy.UpdateDescription(source.Description);
        copy.UpdateResponsibility(source.Responsibility);
        copy.UpdateType(source.Type);
        copy.ReplaceProjects(source.Projects);
        return copy;
    }

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
