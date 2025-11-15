using Domain.Customers;
using Domain.Orders;

namespace Application.Common.Interfaces.Queries;

public interface IOrderQueries
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
}