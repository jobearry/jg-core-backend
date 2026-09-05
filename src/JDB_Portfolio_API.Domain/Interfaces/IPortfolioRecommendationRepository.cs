using JDB_Portfolio_API.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.Interfaces;

public interface IPortfolioRecommendationRepository: IRepository<Recommendation>
{
    Task<List<Recommendation?>> GetAllRecommendationsAsync();
}
