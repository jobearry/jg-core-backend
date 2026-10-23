using System;
using System.Collections.Generic;
using JDB_Portfolio_API.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace JDB_Portfolio_API.Persistence.Contexts;

public partial class PortfolioContext : DbContext
{
    public PortfolioContext(DbContextOptions<PortfolioContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ExperienceEntity> Experiences { get; set; }

    public virtual DbSet<ProjectEntity> Projects { get; set; }

    public virtual DbSet<RecommendationEntity> Recommendations { get; set; }

    public virtual DbSet<TechStackEntity> TechStacks { get; set; }

    public virtual DbSet<TechStackCategoryEntity> TechStackCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One IEntityTypeConfiguration<T> per entity in Configuration/, so this file
        // stays a DbContext and a schema change diffs in the file it belongs to.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortfolioContext).Assembly);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
