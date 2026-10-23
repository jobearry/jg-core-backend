using JDB_Portfolio_API.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JDB_Portfolio_API.Persistence.Configurations;

/// <summary>
/// Join entity for the many-to-many relationship between projects and tech stacks.
/// It has no <c>DbSet</c>; it is reachable through the navigation properties.
/// </summary>
public class ProjectTechStackConfiguration : IEntityTypeConfiguration<ProjectTechStack>
{
    public void Configure(EntityTypeBuilder<ProjectTechStack> e)
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
    }
}
