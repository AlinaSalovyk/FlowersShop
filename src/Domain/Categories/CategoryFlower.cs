using Domain.Flowers;

namespace Domain.Categories;

public class CategoryFlower
{
    public CategoryId CategoryId { get; }
    public Category? Category { get; private set; }

    public FlowerId FlowerId { get; }
    public Flower? Flower { get; private set; }

    private CategoryFlower(CategoryId categoryId, FlowerId flowerId)
        => (CategoryId, FlowerId) = (categoryId, flowerId);

    public static CategoryFlower New(CategoryId categoryId, FlowerId flowerId)
        => new(categoryId, flowerId);
}