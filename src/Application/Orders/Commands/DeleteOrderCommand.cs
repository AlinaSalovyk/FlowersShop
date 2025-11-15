using Application.Common.Interfaces.Repositories;
using Application.Orders.Exceptions;
using Domain.Orders;
using LanguageExt;
using MediatR;

namespace Application.Orders.Commands;

public record DeleteOrderCommand : IRequest<Either<OrderException, Order>>
{
    public required Guid OrderId { get; init; }
}

public class DeleteOrderCommandHandler(IOrderRepository orderRepository)
    : IRequestHandler<DeleteOrderCommand, Either<OrderException, Order>>
{
    public async Task<Either<OrderException, Order>> Handle(
        DeleteOrderCommand request,
        CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var existingOrder = await orderRepository.GetByIdAsync(orderId, cancellationToken);

        return await existingOrder.MatchAsync(
            o => DeleteEntity(o, cancellationToken),
            () => Task.FromResult<Either<OrderException, Order>>(
                new OrderNotFoundException(orderId)));
    }

    private async Task<Either<OrderException, Order>> DeleteEntity(
        Order order,
        CancellationToken cancellationToken)
    {
        try
        {
            return await orderRepository.DeleteAsync(order, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledOrderException(order.Id, exception);
        }
    }
}