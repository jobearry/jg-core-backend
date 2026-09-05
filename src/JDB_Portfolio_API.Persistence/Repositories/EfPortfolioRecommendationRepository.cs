using AutoMapper;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Persistence.Contexts;
using JDB_Portfolio_API.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Persistence.Repositories;

public class EfPortfolioRecommendationRepository:BasePortfolioRepository<Recommendation, RecommendationEntity>, IPortfolioRecommendationRepository
{
    public EfPortfolioRecommendationRepository(PortfolioContext context, IMapper mapper)
    : base(context, mapper) { }

    public async Task<List<Recommendation?>> GetAllRecommendationsAsync()
    {
        var recommendationEntities = await _dbSet.ToListAsync();
        return _mapper.Map<List<Recommendation?>>(recommendationEntities);
    }
}
