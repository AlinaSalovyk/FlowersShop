using Domain.Orders;

namespace Api.Dtos;

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<OrderItemDto> Items)
{
    public static OrderDto FromDomainModel(Order order)
        => new(
            order.Id.Value,
            order.CustomerId.Value,
            order.Customer != null ? $"{order.Customer.FirstName} {order.Customer.LastName}" : "Unknown",
            order.TotalAmount,
            order.Status.ToString(),
            order.CreatedAt,
            order.UpdatedAt,
            order.Items?.Select(OrderItemDto.FromDomainModel).ToList() ?? []);
}

public record OrderItemDto(
    Guid Id,
    Guid FlowerId,
    string FlowerName,
    int Quantity,
    decimal Price,
    decimal Subtotal)
{
    public static OrderItemDto FromDomainModel(OrderItem item)
        => new(
            item.Id.Value,
            item.FlowerId.Value,
            item.Flower?.Name ?? "Unknown",
            item.Quantity,
            item.Price,
            item.Price * item.Quantity);
}

public record CreateOrderDto(
    Guid CustomerId,
    IReadOnlyList<CreateOrderItemDto> Items);

public record CreateOrderItemDto(
    Guid FlowerId,
    int Quantity);

public record UpdateOrderStatusDto(string Status);