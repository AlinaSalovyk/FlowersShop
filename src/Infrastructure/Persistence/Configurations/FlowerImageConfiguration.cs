using Domain.Flowers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class FlowerImageConfiguration : IEntityTypeConfiguration<FlowerImage>
{
    public void Configure(EntityTypeBuilder<FlowerImage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new FlowerImageId(x));

        builder.Property(x => x.OriginalName).IsRequired().HasColumnType("varchar(255)");

        builder.Property(x => x.FlowerId).HasConversion(x => x.Value, x => new FlowerId(x));
        builder.HasOne<Flower>()
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.FlowerId)
            .HasConstraintName("fk_flower_images_flowers_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}