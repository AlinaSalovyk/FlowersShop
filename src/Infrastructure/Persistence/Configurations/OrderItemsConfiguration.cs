using Domain.Flowers;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrderItemsConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new OrderItemId(x));

        builder.Property(x => x.OrderId).HasConversion(x => x.Value, x => new OrderId(x));
        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.FlowerId).HasConversion(x => x.Value, x => new FlowerId(x));
        builder.HasOne(x => x.Flower)
            .WithMany()
            .HasForeignKey(x => x.FlowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
    }
}