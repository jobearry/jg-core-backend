using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioProjectService
{
    private readonly IPortfolioProjectRepository _projectRepo;
    public PortfolioProjectService(IPortfolioProjectRepository projectRepo)
    {
        _projectRepo = projectRepo;
    }

    public async Task<Result<List<Project?>>> GetAllAsync(bool includeTools)
    {
        try
        {
            List<Project?> projects = includeTools ? await _projectRepo.GetAllProjectsWithTechStacksAsync() : await _projectRepo.GetAllAsync();
            return Result<List<Project?>>.Success(projects.OrderBy(x => x.Id).ToList());
        }
        catch (HttpRequestException hre)
        {
            return Result<List<Project?>>.Failure("Failed to get portfolio project content: " + hre.Message);
        }
    }
}
