using JDB_Portfolio_API.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Domain.Interfaces;

public interface IPortfolioExperienceRepository : IRepository<Experience>
{
    //Task<List<DTOProject>> GetExperienceProjectsAsync(int experienceId);
}