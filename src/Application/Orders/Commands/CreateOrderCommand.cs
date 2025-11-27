using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Orders.Exceptions;
using Domain.Customers;
using Domain.Flowers;
using Domain.Orders;
using LanguageExt;
using MediatR;

namespace Application.Orders.Commands;

public record CreateOrderCommand : IRequest<Either<OrderException, Order>>
{
    public required Guid CustomerId { get; init; }
    public required IReadOnlyList<OrderItemDto> Items { get; init; }
}

public record OrderItemDto
{
    public required Guid FlowerId { get; init; }
    public required int Quantity { get; init; }
}

public class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IFlowerRepository flowerRepository,
    IApplicationDbContext dbContext) 
    : IRequestHandler<CreateOrderCommand, Either<OrderException, Order>>
{
    public async Task<Either<OrderException, Order>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Items.Any())
        {
            return new OrderEmptyException(OrderId.Empty());
        }

        var customerId = new CustomerId(request.CustomerId);
        var customerOption = await customerRepository.GetByIdAsync(customerId, cancellationToken);

        return await customerOption.MatchAsync(
            c => CreateOrderWithTransaction(c.Id, request, cancellationToken),
            () => Task.FromResult<Either<OrderException, Order>>(
                new OrderCustomerNotFoundException(OrderId.Empty())));
    }

    private async Task<Either<OrderException, Order>> CreateOrderWithTransaction(
        CustomerId customerId,
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {

        using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);

        try
        {
            var flowerIds = request.Items
                .Select(i => new FlowerId(i.FlowerId))
                .Distinct()
                .ToList();

            var flowers = await flowerRepository.GetByIdsAsync(flowerIds, cancellationToken);

            var flowersMap = flowers.ToDictionary(f => f.Id);

            if (flowers.Count != flowerIds.Count)
            {
                return new OrderFlowerNotFoundException(OrderId.Empty()); 
            }

            var orderId = OrderId.New();
            var orderItems = new List<OrderItem>();

            foreach (var itemDto in request.Items)
            {
                var flowerId = new FlowerId(itemDto.FlowerId);
                var flower = flowersMap[flowerId];
                
                if (flower.StockQuantity < itemDto.Quantity)
                {
                    return new InsufficientStockForOrderException(
                        orderId,
                        flowerId.Value,
                        flower.Name,
                        itemDto.Quantity,
                        flower.StockQuantity);
                }

                flower.DecreaseStock(itemDto.Quantity);

                orderItems.Add(OrderItem.New(orderId, flowerId, itemDto.Quantity, flower.Price));
                
                await flowerRepository.UpdateAsync(flower, cancellationToken);
            }

            var order = Order.New(orderId, customerId, orderItems);
            await orderRepository.AddAsync(order, cancellationToken);
            
            transaction.Commit();

            return order;
        }
        catch (Exception exception)
        {
            return new UnhandledOrderException(OrderId.Empty(), exception);
        }
    }
}