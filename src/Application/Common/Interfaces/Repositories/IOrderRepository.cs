using Domain.Orders;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order> AddAsync(Order entity, CancellationToken cancellationToken);
    Task<Order> UpdateAsync(Order entity, CancellationToken cancellationToken);
    Task<Order> DeleteAsync(Order entity, CancellationToken cancellationToken);
    Task<Option<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken);
}