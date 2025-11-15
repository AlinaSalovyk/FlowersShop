using Domain.Categories;
using Domain.Flowers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CategoryFlowersConfiguration : IEntityTypeConfiguration<CategoryFlower>
{
    public void Configure(EntityTypeBuilder<CategoryFlower> builder)
    {
        builder.HasKey(cf => new { cf.CategoryId, cf.FlowerId });

        builder.Property(x => x.CategoryId).HasConversion(x => x.Value, x => new CategoryId(x));
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Flowers)
            .HasForeignKey(x => x.CategoryId)
            .HasConstraintName("fk_category_flowers_categories_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.FlowerId).HasConversion(x => x.Value, x => new FlowerId(x));
        builder.HasOne(x => x.Flower)
            .WithMany(x => x.Categories)
            .HasForeignKey(x => x.FlowerId)
            .HasConstraintName("fk_category_flowers_flowers_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}