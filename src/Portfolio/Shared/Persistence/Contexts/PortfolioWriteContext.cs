using Microsoft.EntityFrameworkCore;
using Portfolio.Persistence.Entities;

namespace Portfolio.Persistence.Contexts;

/// <summary>
/// Bound to the DML-only runtime credential. Migrations use <see cref="PortfolioContext"/>;
/// this context is for admin write commands only.
/// </summary>
public sealed class PortfolioWriteContext : DbContext
{
    public PortfolioWriteContext(DbContextOptions<PortfolioWriteContext> options)
        : base(options)
    {
    }

    public DbSet<ExperienceEntity> Experiences { get; set; }

    public DbSet<ProjectEntity> Projects { get; set; }

    public DbSet<RecommendationEntity> Recommendations { get; set; }

    public DbSet<TechStackEntity> TechStacks { get; set; }

    public DbSet<TechStackCategoryEntity> TechStackCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortfolioContext).Assembly);
}
