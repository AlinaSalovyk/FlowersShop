using Domain.Flowers;

namespace Api.Dtos;

public record FlowerDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<CategoryFlowerDto>? Categories)
{
    public static FlowerDto FromDomainModel(Flower flower)
        => new(
            flower.Id.Value,
            flower.Name,
            flower.Description,
            flower.Price,
            flower.StockQuantity,
            flower.CreatedAt,
            flower.UpdatedAt,
            flower.Categories != null
                ? flower.Categories.Select(CategoryFlowerDto.FromDomainModel).ToList()
                : []);
}

public record CreateFlowerDto(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    IReadOnlyList<Guid> Categories);

public record UpdateFlowerDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    IReadOnlyList<Guid> Categories);