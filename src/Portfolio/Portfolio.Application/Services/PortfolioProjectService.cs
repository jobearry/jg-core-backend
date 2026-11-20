using Portfolio.Application.Common;
using Portfolio.Application.DTO;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Application.Services;

public class PortfolioProjectService : PortfolioServiceBase<Project, IPortfolioProjectRepository>
{
    public PortfolioProjectService(IPortfolioProjectRepository repository, ILogger<PortfolioProjectService> logger)
        : base(repository, logger) { }

    public async Task<Result<List<ProjectDto>>> GetAllProjectsAsync(
        bool includeTools,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async ct =>
        {
            var projects = includeTools
                ? await Repository.GetAllProjectsWithTechStacksAsync(ct)
                : await Repository.GetAllAsync(ct);

            return projects
                .OrderBy(x => x.Id)
                .Select(x => x.ToDto())
                .ToList();
        }, "get portfolio project content", cancellationToken);
    }
}
