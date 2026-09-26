using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioSkillService:BaseService<TechStack>
{
    private readonly IPortfolioTechStackRepository _stackRepo;
    public PortfolioSkillService(IPortfolioTechStackRepository stackRepo)
        : base(stackRepo)
    {
        _stackRepo = stackRepo;
    }

    public async Task<Result<List<TechStack?>>> GetAllTechStacksAsync()
    {
        try
        {
            List<TechStack?> techStacks = await _stackRepo.GetAllTechStacksAsync() ?? await _repo.GetAllAsync();
            return Result<List<TechStack?>>.Success(techStacks.OrderBy(x => x.Id).ToList());
        }
        catch (HttpRequestException hre)
        {
            return Result<List<TechStack?>>.Failure("Failed to get portfolio tech stack content: " + hre.Message);
        }
    }
}
