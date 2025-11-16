using Domain.Customers;

namespace Tests.Data.Customers;

public static class CustomersData
{
    public static Customer FirstTestCustomer()
        => Customer.New(
            CustomerId.New(),
            "John",
            "Doe",
            "john.doe@example.com",
            "+380501234567",
            "123 Main St, Kyiv");

    public static Customer SecondTestCustomer()
        => Customer.New(
            CustomerId.New(),
            "Jane",
            "Smith",
            "jane.smith@example.com",
            "+380509876543",
            "456 Oak Ave, Lviv");

    public static Customer ThirdTestCustomer()
        => Customer.New(
            CustomerId.New(),
            "Bob",
            "Johnson",
            "bob.johnson@example.com",
            "+380671234567",
            "789 Pine Rd, Odesa");
}