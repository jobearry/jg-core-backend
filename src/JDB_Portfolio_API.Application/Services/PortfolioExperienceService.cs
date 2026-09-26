using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioExperienceService: BaseService<Experience>
{
    private readonly IPortfolioExperienceRepository _portfolioExperienceRepository;
    public PortfolioExperienceService(IPortfolioExperienceRepository portfolioExperienceRepository)
        : base(portfolioExperienceRepository) 
    {
        _portfolioExperienceRepository = portfolioExperienceRepository;
    }

    public async Task<Result<List<Experience?>>> GetAllExperienceAsync(bool includeProjects)
    {
        try
        {
            List<Experience?> experiences = includeProjects ? 
                await _portfolioExperienceRepository.GetAllExperienceWithProjectsAsync() : await _repo.GetAllAsync();

            return Result<List<Experience?>>.Success(experiences.OrderByDescending(x => x.Id).ToList());
        }
        catch (HttpRequestException hre)
        {
            return Result<List<Experience?>>.Failure("Failed to get portfolio experience content: " + hre.Message);
        }
    }
}
