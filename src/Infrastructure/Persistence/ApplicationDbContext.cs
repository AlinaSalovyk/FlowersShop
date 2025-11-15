using System.Data;
using System.Reflection;
using Application.Common.Interfaces;
using Domain.Categories;
using Domain.Customers;
using Domain.Flowers;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Flower> Flowers { get; init; }
    public DbSet<Category> Categories { get; init; }
    public DbSet<CategoryFlower> CategoryFlowers { get; init; }
    public DbSet<FlowerImage> FlowerImages { get; init; }
    public DbSet<Customer> Customers { get; init; }
    public DbSet<Order> Orders { get; init; }
    public DbSet<OrderItem> OrderItems { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        return transaction.GetDbTransaction();
    }
}