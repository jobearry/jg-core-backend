using System;
using System.Collections.Generic;
using JDB_Portfolio_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JDB_Portfolio_API.Persistence.Contexts;

public partial class PortfolioContext : DbContext
{
    public PortfolioContext(DbContextOptions<PortfolioContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Experience> Experiences { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Recommendation> Recommendations { get; set; }

    public virtual DbSet<TechStack> TechStacks { get; set; }

    public virtual DbSet<TechStackCategory> TechStackCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Experience>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("experience_pkey");

            entity.ToTable("experience");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(255)
                .HasColumnName("company_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Responsibility).HasColumnName("responsibility");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasColumnName("role");
            entity.Property(e => e.StartedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("started_at");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("project_pkey");

            entity.ToTable("project");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Contribution).HasColumnName("contribution");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ExperienceId).HasColumnName("experience_id");
            entity.Property(e => e.ProjectName).HasColumnName("project_name");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recommendations_pkey");

            entity.ToTable("recommendations");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CareerPosition)
                .HasMaxLength(50)
                .HasColumnName("career_position");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PersonName)
                .HasMaxLength(50)
                .HasColumnName("person_name");
            entity.Property(e => e.PostedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("posted_at");
        });

        modelBuilder.Entity<TechStack>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tech_stack_pkey");

            entity.ToTable("tech_stack");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ImgSrc)
                .HasMaxLength(255)
                .HasColumnName("img_src");
            entity.Property(e => e.StackName)
                .HasMaxLength(255)
                .HasColumnName("stack_name");
            entity.Property(e => e.TechStackCategoryId).HasColumnName("tech_stack_category_id");
        });

        modelBuilder.Entity<TechStackCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tech_stack_category_pkey");

            entity.ToTable("tech_stack_category");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(255)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
