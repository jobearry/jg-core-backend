using Portfolio.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Portfolio.Persistence.Configurations;

public class RecommendationEntityConfiguration : IEntityTypeConfiguration<RecommendationEntity>
{
    public void Configure(EntityTypeBuilder<RecommendationEntity> entity)
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
    }
}
