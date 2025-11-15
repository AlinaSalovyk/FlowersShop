using Domain.Customers;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer> AddAsync(Customer entity, CancellationToken cancellationToken);
    Task<Customer> UpdateAsync(Customer entity, CancellationToken cancellationToken);
    Task<Customer> DeleteAsync(Customer entity, CancellationToken cancellationToken);
    Task<Option<Customer>> GetByIdAsync(CustomerId id, CancellationToken cancellationToken);
    Task<Option<Customer>> GetByEmailAsync(string email, CancellationToken cancellationToken);
}