using Domain.Flowers;

namespace Domain.Orders;

public class OrderItem
{
    public OrderItemId Id { get; }
    public OrderId OrderId { get; }
    public Order? Order { get; private set; }
    
    public FlowerId FlowerId { get; }
    public Flower? Flower { get; private set; }
    
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }

    private OrderItem(OrderItemId id, OrderId orderId, FlowerId flowerId, int quantity, decimal price)
    {
        Id = id;
        OrderId = orderId;
        FlowerId = flowerId;
        Quantity = quantity;
        Price = price;
    }

    public static OrderItem New(OrderId orderId, FlowerId flowerId, int quantity, decimal price)
        => new(OrderItemId.New(), orderId, flowerId, quantity, price);
}