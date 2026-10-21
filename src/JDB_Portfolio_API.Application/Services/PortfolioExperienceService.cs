using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioExperienceService : PortfolioServiceBase<Experience, IPortfolioExperienceRepository>
{
    public PortfolioExperienceService(IPortfolioExperienceRepository repository)
        : base(repository) { }

    public async Task<Result<List<Experience>>> GetAllExperienceAsync(bool includeProjects)
    {
        return await ExecuteAsync(async () =>
        {
            var experiences = includeProjects
                ? await Repository.GetAllExperienceWithProjectsAsync()
                : await Repository.GetAllAsync();

            return experiences.OrderByDescending(x => x.Id).ToList();
        }, "get portfolio experience content");
    }
}
