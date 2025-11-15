using Domain.Categories;

namespace Domain.Flowers;

public class Flower
{
    public FlowerId Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<FlowerImage>? Images { get; private set; } = [];
    public ICollection<CategoryFlower>? Categories { get; private set; } = [];

    private Flower(FlowerId id, string name, string description, decimal price, int stockQuantity, DateTime createdAt, DateTime? updatedAt)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Flower New(
        FlowerId id,
        string name,
        string description,
        decimal price,
        int stockQuantity,
        ICollection<CategoryFlower> categories)
        => new(id, name, description, price, stockQuantity, DateTime.UtcNow, null)
        {
            Categories = categories
        };

    public void UpdateDetails(string name, string description, decimal price, int stockQuantity)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStock(int quantity)
    {
        StockQuantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecreaseStock(int quantity)
    {
        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock");
        
        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}