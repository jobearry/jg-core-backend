using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JDB_Portfolio_API.Application.Services;

namespace JDB_Portfolio_API.Endpoints;
public static class PortfolioOverview
{
    public static void MapOverview(this IEndpointRouteBuilder routes){
        routes.MapGet("api/v1/overview", async (PortfolioService portfolioService) =>
        {
            var result = await portfolioService.GetOverview();
            if (!result.IsSuccess)
            {
                return Results.Problem(result.Error, statusCode: 502);
            }
            return Results.Ok(result.Value);
        })
        .WithTags("Portfolio");
    }
}