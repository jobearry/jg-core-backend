using Portfolio.Domain.Entities;
using Portfolio.Features.Recommendation;

namespace Portfolio.Api.Tests.Fakes;

/// <summary>
/// In-memory recommendation repository. <see cref="Failure"/> lets handler and endpoint
/// tests exercise the infrastructure failure path without touching PostgreSQL.
/// </summary>
public sealed class FakeRecommendationRepository : IRecommendationRepository
{
    public List<Recommendation> Recommendations { get; } = new();

    public Exception? Failure { get; set; }

    public Task<List<Recommendation>> GetAllAsync(CancellationToken cancellationToken)
    {
        ThrowIfFailed();
        return Task.FromResult(Recommendations.ToList());
    }

    public Task<Recommendation?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        ThrowIfFailed();
        return Task.FromResult(Recommendations.FirstOrDefault(item => item.Id == id));
    }

    public Task<Recommendation> AddAsync(Recommendation recommendation, CancellationToken cancellationToken)
    {
        ThrowIfFailed();
        var created = new Recommendation(
            Recommendations.Count == 0 ? 1 : Recommendations.Max(item => item.Id) + 1,
            recommendation.PersonName,
            recommendation.Description,
            recommendation.CareerPosition,
            recommendation.PostedAt);
        Recommendations.Add(created);
        return Task.FromResult(created);
    }

    public Task<Recommendation> UpdateAsync(Recommendation recommendation, CancellationToken cancellationToken)
    {
        ThrowIfFailed();
        var index = Recommendations.FindIndex(item => item.Id == recommendation.Id);
        if (index >= 0)
        {
            Recommendations[index] = recommendation;
        }

        return Task.FromResult(recommendation);
    }

    public Task DeleteAsync(Recommendation recommendation, CancellationToken cancellationToken)
    {
        ThrowIfFailed();
        Recommendations.RemoveAll(item => item.Id == recommendation.Id);
        return Task.CompletedTask;
    }

    private void ThrowIfFailed()
    {
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}
