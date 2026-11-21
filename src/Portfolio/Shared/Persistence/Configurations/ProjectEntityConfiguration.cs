using Portfolio.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Portfolio.Persistence.Configurations;

public class ProjectEntityConfiguration : IEntityTypeConfiguration<ProjectEntity>
{
    public void Configure(EntityTypeBuilder<ProjectEntity> entity)
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
    }
}
