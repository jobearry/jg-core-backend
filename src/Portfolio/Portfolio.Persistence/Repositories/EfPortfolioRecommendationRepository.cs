using AutoMapper;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Persistence.Contexts;
using Portfolio.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Portfolio.Persistence.Repositories;

public class EfPortfolioRecommendationRepository:BasePortfolioRepository<Recommendation, RecommendationEntity>, IPortfolioRecommendationRepository
{
    public EfPortfolioRecommendationRepository(PortfolioContext context, IMapper mapper)
    : base(context, mapper) { }

    public async Task<List<Recommendation>> GetAllRecommendationsAsync(CancellationToken cancellationToken)
    {
        var recommendationEntities = await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
        return _mapper.Map<List<Recommendation>>(recommendationEntities);
    }
}
