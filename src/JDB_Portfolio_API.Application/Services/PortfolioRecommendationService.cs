using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioRecommendationService:BaseService<Recommendation>
{
    private readonly IPortfolioRecommendationRepository _recoRepo;
    public PortfolioRecommendationService(IPortfolioRecommendationRepository recoRepo)
        : base(recoRepo)
    {
        _recoRepo = recoRepo;
    }

    public async Task<Result<List<Recommendation?>>> GetAllRecommendationsAsync()
    {
        try
        {
            List<Recommendation?> recommendations = await _recoRepo.GetAllRecommendationsAsync()?? await _repo.GetAllAsync();
            return Result<List<Recommendation?>>.Success(recommendations.OrderBy(x => x.Id).ToList());
        }
        catch (HttpRequestException hre)
        {
            return Result<List<Recommendation?>>.Failure("Failed to get portfolio recommendation content: " + hre.Message);
        }
    }
}
