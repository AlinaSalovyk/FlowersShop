using Domain.Orders;

namespace Domain.Customers;

public class Customer
{
    public CustomerId Id { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }
    public string Address { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<Order>? Orders { get; private set; } = [];

    private Customer(CustomerId id, string firstName, string lastName, string email, string phone, string address, DateTime createdAt, DateTime? updatedAt)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        Address = address;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Customer New(CustomerId id, string firstName, string lastName, string email, string phone, string address)
        => new(id, firstName, lastName, email, phone, address, DateTime.UtcNow, null);

    public void UpdateDetails(string firstName, string lastName, string email, string phone, string address)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }
}