using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Customers;
using Domain.Orders;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OrderRepository(ApplicationDbContext context) : IOrderRepository, IOrderQueries
{
    public async Task<Order> AddAsync(Order entity, CancellationToken cancellationToken)
    {
        await context.Orders.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Order> UpdateAsync(Order entity, CancellationToken cancellationToken)
    {
        context.Orders.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Order> DeleteAsync(Order entity, CancellationToken cancellationToken)
    {
        context.Orders.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Option<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Include(x => x.Items)!
            .ThenInclude(x => x.Flower)
            .Include(x => x.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<Order>.None;
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Orders
            .Include(x => x.Items)!
            .ThenInclude(x => x.Flower)
            .Include(x => x.Customer)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken)
    {
        return await context.Orders
            .Include(x => x.Items)!
            .ThenInclude(x => x.Flower)
            .Where(x => x.CustomerId == customerId)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await context.Orders
            .Include(x => x.Items)!
            .ThenInclude(x => x.Flower)
            .Include(x => x.Customer)
            .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}