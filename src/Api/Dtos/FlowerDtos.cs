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
    IReadOnlyList<CategoryFlowerDto>? Categories,
    IReadOnlyList<FlowerImageDto>? Images)
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
                : [],
            flower.Images != null
                ? flower.Images.Select(FlowerImageDto.FromDomainModel).ToList()
                : []);
}

public record FlowerImageDto(
    Guid Id,
    string OriginalName,
    string FilePath)
{
    public static FlowerImageDto FromDomainModel(FlowerImage image)
        => new(
            image.Id.Value,
            image.OriginalName,
            image.GetFilePath());
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