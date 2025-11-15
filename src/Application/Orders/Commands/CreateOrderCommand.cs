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
    IFlowerRepository flowerRepository)
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
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken);

        return await customer.MatchAsync(
            c => CreateEntity(c.Id, request, cancellationToken),
            () => Task.FromResult<Either<OrderException, Order>>(
                new OrderCustomerNotFoundException(OrderId.Empty())));
    }

    private async Task<Either<OrderException, Order>> CreateEntity(
        CustomerId customerId,
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var orderId = OrderId.New();
            var orderItems = new List<OrderItem>();

            // Спочатку перевіряємо всі квіти та їх наявність
            foreach (var item in request.Items)
            {
                var flowerId = new FlowerId(item.FlowerId);
                var flowerOption = await flowerRepository.GetByIdAsync(flowerId, cancellationToken);

                if (flowerOption.IsNone)
                {
                    return new OrderFlowerNotFoundException(orderId);
                }

                var flower = flowerOption.Match(f => f, () => throw new InvalidOperationException());

                // Перевіряємо наявність на складі
                if (flower.StockQuantity < item.Quantity)
                {
                    return new InsufficientStockForOrderException(
                        orderId, 
                        flowerId.Value, 
                        flower.Name, 
                        item.Quantity, 
                        flower.StockQuantity);
                }
            }

            // Якщо все ОК, створюємо замовлення та зменшуємо запаси
            foreach (var item in request.Items)
            {
                var flowerId = new FlowerId(item.FlowerId);
                var flowerOption = await flowerRepository.GetByIdAsync(flowerId, cancellationToken);
                var flower = flowerOption.Match(f => f, () => throw new InvalidOperationException());

                // Зменшуємо кількість на складі
                flower.DecreaseStock(item.Quantity);
                await flowerRepository.UpdateAsync(flower, cancellationToken);

                orderItems.Add(OrderItem.New(orderId, flowerId, item.Quantity, flower.Price));
            }

            var order = await orderRepository.AddAsync(
                Order.New(orderId, customerId, orderItems),
                cancellationToken);

            return order;
        }
        catch (InvalidOperationException ex)
        {
            return new UnhandledOrderException(OrderId.Empty(), ex);
        }
        catch (Exception exception)
        {
            return new UnhandledOrderException(OrderId.Empty(), exception);
        }
    }
}