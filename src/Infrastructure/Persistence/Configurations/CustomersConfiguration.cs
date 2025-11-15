using Domain.Customers;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CustomersConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new CustomerId(x));

        builder.Property(x => x.FirstName)
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.Phone)
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(x => x.Address)
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);
    }
}