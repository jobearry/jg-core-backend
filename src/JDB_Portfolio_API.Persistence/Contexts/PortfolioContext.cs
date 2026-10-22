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
        // Configure the many-to-many relationship between ProjectEntity and TechStackEntity
        modelBuilder.Entity<ProjectTechStack>(e =>
        {
            e.HasKey(pts => new { pts.ProjectId, pts.TechStackId }).HasName("project_tech_stack_pkey");
            e.Property(e => e.ProjectId).HasColumnName("project_id");
            e.Property(e => e.TechStackId).HasColumnName("tech_stack_id");
            e.ToTable("project_tech_stack");
            e.HasOne(pt => pt.Project)
                .WithMany(p => p.ProjectTechStacks)
                .HasForeignKey(pt => pt.ProjectId);
            e.HasOne(pt => pt.TechStack)
                .WithMany(t => t.ProjectTechStacks)
                .HasForeignKey(pt => pt.TechStackId);
        });

        //
        modelBuilder.Entity<ExperienceEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("experience_pkey");

            entity.ToTable("experience", table =>
                // Plan 3.1a. NOT NULL alone would still allow an empty string, which is
                // the same loophole `role` has — a column that demands a value, filled
                // with "" when there is not one. btrim also rejects all-whitespace.
                table.HasCheckConstraint(
                    "ck_experience_company_name_not_blank",
                    "btrim(company_name) <> ''"));

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(255)
                .IsRequired()
                .HasColumnName("company_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Responsibility).HasColumnName("responsibility");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasColumnName("role");
            entity.Property(e => e.StartedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("started_at");
            entity.Property(e => e.FinishedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("finished_at");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
        });

        modelBuilder.Entity<ProjectEntity>(entity =>
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

            entity.HasOne(p => p.Experience)
                  .WithMany(e => e.Projects)
                  .HasForeignKey(p => p.ExperienceId)
                  .HasConstraintName("FK_project_experience_experience_id")
                  .OnDelete(DeleteBehavior.SetNull);

        });

        modelBuilder.Entity<RecommendationEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recommendation_pkey");

            entity.ToTable("recommendation");

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

        modelBuilder.Entity<TechStackEntity>(entity =>
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

            entity.HasOne(t => t.TechStackCategory)
                  .WithMany(c => c.TechStacks)
                  .HasForeignKey(t => t.TechStackCategoryId)
                  .HasConstraintName("FK_tech_stack_tech_stack_category_id");
        });

        modelBuilder.Entity<TechStackCategoryEntity>(entity =>
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
