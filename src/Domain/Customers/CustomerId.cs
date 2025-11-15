namespace Domain.Customers;

public record CustomerId(Guid Value)
{
    public static CustomerId Empty() => new(Guid.Empty);
    public static CustomerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}