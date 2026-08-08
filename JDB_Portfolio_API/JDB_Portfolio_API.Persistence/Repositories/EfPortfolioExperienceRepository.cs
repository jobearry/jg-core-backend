using AutoMapper;
using JDB_Portfolio_API.Application.Common;
using JDB_Portfolio_API.Domain.Entities;
using JDB_Portfolio_API.Domain.Interfaces;
using JDB_Portfolio_API.Domain.ValueObjects;
using JDB_Portfolio_API.Persistence.Contexts;
using JDB_Portfolio_API.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace JDB_Portfolio_API.Persistence.Repositories;

public class EfPortfolioExperienceRepository: BasePortfolioRepository<Experience, ExperienceEntity>, IPortfolioExperienceRepository
{
    public EfPortfolioExperienceRepository(PortfolioContext context, IMapper mapper)
        : base(context, mapper) { }
}
