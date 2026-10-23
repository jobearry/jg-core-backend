using JDB_Portfolio_API.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JDB_Portfolio_API.Persistence.Configurations;

public class TechStackEntityConfiguration : IEntityTypeConfiguration<TechStackEntity>
{
    public void Configure(EntityTypeBuilder<TechStackEntity> entity)
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
    }
}
