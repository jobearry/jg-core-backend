using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JDB_Portfolio_API.Application.Services;

public class PortfolioSkillService : PortfolioServiceBase<TechStack, IPortfolioTechStackRepository>
{
    public PortfolioSkillService(IPortfolioTechStackRepository repository)
        : base(repository) { }

    public async Task<Result<List<TechStack>>> GetAllTechStacksAsync()
    {
        return await ExecuteAsync(async () =>
        {
            var techStacks = await Repository.GetAllTechStacksAsync();

            return techStacks.OrderBy(x => x.Id).ToList();
        }, "get portfolio tech stack content");
    }
}
