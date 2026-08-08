using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioExperienceServices
{
    private readonly IPortfolioExperienceRepository _expRepo;
    public PortfolioExperienceServices(IPortfolioExperienceRepository expRepo)
    {
        _expRepo = expRepo;
    }

    public async Task<Result<List<Experience>>> GetAllAsync()
    {
        try
        {
            var experiences = await _expRepo.GetAllAsync();
            return Result<List<Experience>>.Success(experiences.OrderBy(x => x.Id).ToList());

        }
        catch (HttpRequestException hre)
        {
            return Result<List<Experience>>.Failure("Failed to get portfolio experience content: " + hre.Message);
        }
    }
}
