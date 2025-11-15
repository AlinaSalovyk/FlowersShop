namespace Domain.Flowers;

public record FlowerId(Guid Value)
{
    public static FlowerId Empty() => new(Guid.Empty);
    public static FlowerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}