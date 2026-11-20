using Portfolio.Application.Abstractions;
using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Application.Models;
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
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(
        IPageContentReader pageContentReader,
        ILogger<PortfolioService> logger)
    {
        _pageContentReader = pageContentReader;
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

}
