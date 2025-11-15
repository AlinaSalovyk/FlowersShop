using Domain.Customers;

namespace Api.Dtos;

public record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static CustomerDto FromDomainModel(Customer customer)
        => new(
            customer.Id.Value,
            customer.FirstName,
            customer.LastName,
            customer.Email,
            customer.Phone,
            customer.Address,
            customer.CreatedAt,
            customer.UpdatedAt);
}

public record CreateCustomerDto(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Address);

public record UpdateCustomerDto(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Address);