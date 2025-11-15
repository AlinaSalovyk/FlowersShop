using Application.Common.Interfaces.Repositories;
using Application.Orders.Exceptions;
using Domain.Orders;
using LanguageExt;
using MediatR;

namespace Application.Orders.Commands;

public record UpdateOrderStatusCommand : IRequest<Either<OrderException, Order>>
{
    public required Guid OrderId { get; init; }
    public required OrderStatus Status { get; init; }
}

public class UpdateOrderStatusCommandHandler(IOrderRepository orderRepository)
    : IRequestHandler<UpdateOrderStatusCommand, Either<OrderException, Order>>
{
    public async Task<Either<OrderException, Order>> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var existingOrder = await orderRepository.GetByIdAsync(orderId, cancellationToken);

        return await existingOrder.MatchAsync(
            o => UpdateEntity(o, request, cancellationToken),
            () => Task.FromResult<Either<OrderException, Order>>(
                new OrderNotFoundException(orderId)));
    }

    private async Task<Either<OrderException, Order>> UpdateEntity(
        Order order,
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            order.UpdateStatus(request.Status);
            return await orderRepository.UpdateAsync(order, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledOrderException(order.Id, exception);
        }
    }
}