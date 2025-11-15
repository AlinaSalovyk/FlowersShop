using Domain.Customers;

namespace Application.Customers.Exceptions;

public abstract class CustomerException(CustomerId customerId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public CustomerId CustomerId { get; } = customerId;
}

public class CustomerAlreadyExistException(CustomerId customerId) 
    : CustomerException(customerId, $"Customer already exists under id {customerId}");

public class CustomerNotFoundException(CustomerId customerId) 
    : CustomerException(customerId, $"Customer not found under id {customerId}");

public class CustomerEmailAlreadyExistsException(string email)
    : CustomerException(CustomerId.Empty(), $"Customer with email {email} already exists");

public class UnhandledCustomerException(CustomerId customerId, Exception? innerException = null)
    : CustomerException(customerId, "Unexpected error occurred", innerException);