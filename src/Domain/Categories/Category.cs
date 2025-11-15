namespace Domain.Categories;

public class Category
{
    public CategoryId Id { get; }
    public string Name { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<CategoryFlower>? Flowers { get; private set; } = [];

    private Category(CategoryId id, string name, DateTime createdAt, DateTime? updatedAt)
        => (Id, Name, CreatedAt, UpdatedAt) = (id, name, createdAt, updatedAt);

    public static Category New(CategoryId id, string name)
        => new(id, name, DateTime.UtcNow, null);

    public void UpdateDetails(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}