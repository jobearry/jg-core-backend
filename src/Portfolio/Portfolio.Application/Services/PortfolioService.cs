using Portfolio.Application.Abstractions;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Models;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Features.Project;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Application.Services;

public class PortfolioService
{
    private readonly IPageContentReader _pageContentReader;
    private readonly IProjectRepository _projectRepository;
    private readonly IPortfolioTechStackRepository _techStackRepository;
    private readonly IPortfolioRecommendationRepository _recommendationRepository;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(
        IPageContentReader pageContentReader,
        IProjectRepository projectRepository,
        IPortfolioTechStackRepository techStackRepository,
        IPortfolioRecommendationRepository recommendationRepository,
        ILogger<PortfolioService> logger)
    {
        _pageContentReader = pageContentReader;
        _projectRepository = projectRepository;
        _techStackRepository = techStackRepository;
        _recommendationRepository = recommendationRepository;
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

    public async Task<Result<List<OverviewItemDto>>> GetOverview(CancellationToken cancellationToken)
    {
        try
        {
            // Deliberately sequential. All three repositories share the scoped
            // PortfolioContext, and EF Core forbids concurrent operations on one
            // context — Task.WhenAll here throws "A second operation was started on
            // this context instance". Parallelism needs a context per query, which
            // costs more than it saves for three counts.
            var projects = await _projectRepository.GetAllWithTechStacksAsync(cancellationToken);
            var skills = await _techStackRepository.GetAllTechStacksAsync(cancellationToken);
            var recommendations = await _recommendationRepository.GetAllRecommendationsAsync(cancellationToken);

            // Keys are the contract; the client owns labels, icons and order. Only add a
            // key here when the client has a tile for it.
            var counters = new List<OverviewItemDto>
            {
                new("projects", projects.Count()),
                new("skills", skills.Count()),
                new("recommendations", recommendations.Count())
            };

            return Result<List<OverviewItemDto>>.Success(counters);
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

            return Result<List<OverviewItemDto>>.Failure(error);
        }
    }
}
