using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioService
{
    private readonly INotionService _notionService;
    private readonly IPortfolioProjectRepository _projectRepository;
    private readonly IPortfolioTechStackRepository _techStackRepository;
    public PortfolioService(INotionService notionService, IPortfolioProjectRepository projectRepository, IPortfolioTechStackRepository techStackRepository)
    {
        _notionService = notionService;
        _projectRepository = projectRepository;
        _techStackRepository = techStackRepository;
    }

    public async Task<Result<List<NotionPage>>> GetNotionPageAsync(string pageID)
    {
        try
        {
            var page = await _notionService.QueryPageAsync(pageID);
            return Result<List<NotionPage>>.Success(page);

        }catch (HttpRequestException hre)
        {
           return Result<List<NotionPage>>.Failure("Failed to query Notion page markdown: " + hre.Message);
        }
        catch (InvalidOperationException ioe)
        {
            return Result<List<NotionPage>>.Failure("Failed to deserialize Notion page markdown: " + ioe.Message);
        }
    }

    public async Task<Result<List<DashboardItem>>> GetOverview()
    {
        try
        {
            var projects = await _projectRepository.GetAllProjectsWithTechStacksAsync();
            var skills = await _techStackRepository.GetAllTechStacksAsync();

            var dashboardItems = new List<DashboardItem>
            {
                new DashboardItem
                {
                    Title = "Projects",
                    Count = projects.Count(),
                    Subtitle = "projects across all roles"
                },
                new DashboardItem
                {
                    Title = "Skills",
                    Count = skills.Count(),
                    Subtitle = "tools and technologies"
                }
            };

            return Result<List<DashboardItem>>.Success(dashboardItems);

        }catch (HttpRequestException hre)
        {
           return Result<List<DashboardItem>>.Failure("Failed to query for dashboard overview: " + hre.Message);
        }
    }
}
