namespace Domain.Flowers;

public record FlowerImageId(Guid Value)
{
    public static FlowerImageId Empty() => new(Guid.Empty);
    public static FlowerImageId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}