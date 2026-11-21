using Portfolio.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Portfolio.Persistence.Configurations;

public class TechStackCategoryEntityConfiguration : IEntityTypeConfiguration<TechStackCategoryEntity>
{
    public void Configure(EntityTypeBuilder<TechStackCategoryEntity> entity)
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
    }
}
