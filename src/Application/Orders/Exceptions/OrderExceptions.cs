using Domain.Orders;

namespace Application.Orders.Exceptions;

public abstract class OrderException(OrderId orderId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public OrderId OrderId { get; } = orderId;
}

public class OrderNotFoundException(OrderId orderId) 
    : OrderException(orderId, $"Order not found under id {orderId}");

public class OrderCustomerNotFoundException(OrderId orderId)
    : OrderException(orderId, $"Customer not found for order {orderId}");

public class OrderFlowerNotFoundException(OrderId orderId)
    : OrderException(orderId, $"One or more flowers not found for order {orderId}");

public class OrderEmptyException(OrderId orderId)
    : OrderException(orderId, $"Order {orderId} cannot be empty");

public class InsufficientStockForOrderException(
    OrderId orderId, 
    Guid flowerId, 
    string flowerName, 
    int requested, 
    int available)
    : OrderException(
        orderId, 
        $"Insufficient stock for flower '{flowerName}' (ID: {flowerId}). Requested: {requested}, Available: {available}");

public class UnhandledOrderException(OrderId orderId, Exception? innerException = null)
    : OrderException(orderId, "Unexpected error occurred", innerException);