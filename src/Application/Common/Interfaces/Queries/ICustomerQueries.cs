using Domain.Customers;

namespace Application.Common.Interfaces.Queries;

public interface ICustomerQueries
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken);
}