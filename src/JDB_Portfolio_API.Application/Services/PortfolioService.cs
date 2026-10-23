using JDB_Portfolio_API.Application.Abstractions;
using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Application.Models;
using JDB_Portfolio_API.Application.Settings;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioService
{
    private readonly IPageContentReader _pageContentReader;
    private readonly IPortfolioProjectRepository _projectRepository;
    private readonly IPortfolioTechStackRepository _techStackRepository;
    private readonly IPortfolioRecommendationRepository _recommendationRepository;
    private readonly OverviewSettings _overviewSettings;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(
        IPageContentReader pageContentReader,
        IPortfolioProjectRepository projectRepository,
        IPortfolioTechStackRepository techStackRepository,
        IPortfolioRecommendationRepository recommendationRepository,
        OverviewSettings overviewSettings,
        ILogger<PortfolioService> logger)
    {
        _pageContentReader = pageContentReader;
        _projectRepository = projectRepository;
        _techStackRepository = techStackRepository;
        _recommendationRepository = recommendationRepository;
        _overviewSettings = overviewSettings;
        _logger = logger;
    }

    public async Task<Result<List<NotionPageDto>>> GetNotionPageAsync(
        string pageID,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = await _pageContentReader.ReadPageAsync(pageID, cancellationToken);
            return Result<List<NotionPageDto>>.Success(
                page.Select(notionPage => notionPage.ToDto()).ToList());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A failed request and an unreadable response are both the other side's
            // problem, so both become 502 — but with different codes, so a client can
            // tell "Notion is down" from "Notion changed its format".
            var error = ResultErrors.FromExternalService(ex, "query the Notion page");
            _logger.LogWarning(
                ex,
                "Dependency failure while trying to {Operation} ({ErrorCode}).",
                "query the Notion page",
                error.Code);

            return Result<List<NotionPageDto>>.Failure(error);
        }
    }

    public async Task<Result<List<DashboardItemDto>>> GetOverview(CancellationToken cancellationToken)
    {
        try
        {
            // Deliberately sequential. All three repositories share the scoped
            // PortfolioContext, and EF Core forbids concurrent operations on one
            // context — Task.WhenAll here throws "A second operation was started on
            // this context instance". Parallelism needs a context per query, which
            // costs more than it saves for three counts.
            var projects = await _projectRepository.GetAllProjectsWithTechStacksAsync(cancellationToken);
            var skills = await _techStackRepository.GetAllTechStacksAsync(cancellationToken);
            var recommendations = await _recommendationRepository.GetAllRecommendationsAsync(cancellationToken);

            var projectsText = _overviewSettings.Projects;
            var skillsText = _overviewSettings.Skills;
            var recommendationsText = _overviewSettings.Recommendations;

            var dashboardItems = new List<DashboardItem>
            {
                new DashboardItem
                {
                    Title = projectsText.Title,
                    Count = projects.Count(),
                    Subtitle = projectsText.Subtitle,
                    Description = projectsText.Description
                },
                new DashboardItem
                {
                    Title = skillsText.Title,
                    Count = skills.Count(),
                    Subtitle = skillsText.Subtitle,
                    Description = skillsText.Description
                },
                new DashboardItem
                {
                    Title = recommendationsText.Title,
                    Count = recommendations.Count(),
                    Subtitle = recommendationsText.Subtitle,
                    Description = recommendationsText.Description
                }
            };

            return Result<List<DashboardItemDto>>.Success(
                dashboardItems.Select(item => item.ToDto()).ToList());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // This used to catch HttpRequestException, which these repositories never
            // throw — so a real database failure escaped as an unhandled 500. It is
            // classified and logged properly now.
            var error = ResultErrors.From(ex, "build the dashboard overview");

            if (error.Kind == ErrorKind.Unexpected)
            {
                _logger.LogError(
                    ex,
                    "Unexpected failure while trying to {Operation} ({ErrorCode}).",
                    "build the dashboard overview",
                    error.Code);
            }
            else
            {
                _logger.LogWarning(
                    ex,
                    "Dependency failure while trying to {Operation} ({ErrorCode}).",
                    "build the dashboard overview",
                    error.Code);
            }

            return Result<List<DashboardItemDto>>.Failure(error);
        }
    }
}
