using Portfolio.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Portfolio.Persistence.Configurations;

public class ExperienceEntityConfiguration : IEntityTypeConfiguration<ExperienceEntity>
{
    public void Configure(EntityTypeBuilder<ExperienceEntity> entity)
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
    }
}
