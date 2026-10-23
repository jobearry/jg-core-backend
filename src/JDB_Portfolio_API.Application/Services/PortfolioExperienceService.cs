using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Application.DTO;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioExperienceService : PortfolioServiceBase<Experience, IPortfolioExperienceRepository>
{
    public PortfolioExperienceService(IPortfolioExperienceRepository repository, ILogger<PortfolioExperienceService> logger)
        : base(repository, logger) { }

    public async Task<Result<List<ExperienceDto>>> GetAllExperienceAsync(
        bool includeProjects,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async ct =>
        {
            var experiences = includeProjects
                ? await Repository.GetAllExperienceWithProjectsAsync(ct)
                : await Repository.GetAllAsync(ct);

            return experiences
                .OrderByDescending(x => x.Id)
                .Select(x => x.ToDto())
                .ToList();
        }, "get portfolio experience content", cancellationToken);
    }
}
