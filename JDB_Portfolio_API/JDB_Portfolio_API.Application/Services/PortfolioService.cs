using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioService
{
    private readonly INotionService _notion;
    public PortfolioService(INotionService notion)
    {
        _notion = notion;
    }

    public async Task<Result<List<NotionPage>>> GetPageAsync(string pageID)
    {
        try
        {
            var page = await _notion.QueryPageAsync(pageID);
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
}
