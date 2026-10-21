using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioProjectService : PortfolioServiceBase<Project, IPortfolioProjectRepository>
{
    public PortfolioProjectService(IPortfolioProjectRepository repository)
        : base(repository) { }

    public async Task<Result<List<Project>>> GetAllProjectsAsync(bool includeTools)
    {
        return await ExecuteAsync(async () =>
        {
            var projects = includeTools
                ? await Repository.GetAllProjectsWithTechStacksAsync()
                : await Repository.GetAllAsync();

            return projects.OrderBy(x => x.Id).ToList();
        }, "get portfolio project content");
    }
}
