using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Customers;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CustomerRepository(ApplicationDbContext context) : ICustomerRepository, ICustomerQueries
{
    public async Task<Customer> AddAsync(Customer entity, CancellationToken cancellationToken)
    {
        await context.Customers.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Customer> UpdateAsync(Customer entity, CancellationToken cancellationToken)
    {
        context.Customers.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Customer> DeleteAsync(Customer entity, CancellationToken cancellationToken)
    {
        context.Customers.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Option<Customer>> GetByIdAsync(CustomerId id, CancellationToken cancellationToken)
    {
        var entity = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<Customer>.None;
    }

    public async Task<Option<Customer>> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var entity = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        return entity ?? Option<Customer>.None;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Customers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}