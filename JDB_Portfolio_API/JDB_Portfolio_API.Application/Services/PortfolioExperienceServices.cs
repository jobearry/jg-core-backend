using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
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

    public async Task<List<Experience>> GetAllAsync()
    {
        return await _expRepo.GetAllAsync();
    }
}
