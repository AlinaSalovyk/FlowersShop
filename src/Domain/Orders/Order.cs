using Domain.Customers;

namespace Domain.Orders;

public class Order
{
    public OrderId Id { get; }
    public CustomerId CustomerId { get; }
    public Customer? Customer { get; private set; }
    
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<OrderItem>? Items { get; private set; } = [];

    private Order(OrderId id, CustomerId customerId, decimal totalAmount, OrderStatus status, DateTime createdAt, DateTime? updatedAt)
    {
        Id = id;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Order New(OrderId id, CustomerId customerId, ICollection<OrderItem> items)
    {
        var totalAmount = items.Sum(x => x.Price * x.Quantity);
        return new(id, customerId, totalAmount, OrderStatus.Pending, DateTime.UtcNow, null)
        {
            Items = items
        };
    }

    public void UpdateStatus(OrderStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecalculateTotalAmount()
    {
        TotalAmount = Items?.Sum(x => x.Price * x.Quantity) ?? 0;
        UpdatedAt = DateTime.UtcNow;
    }
}