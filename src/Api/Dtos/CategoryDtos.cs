using Domain.Categories;

namespace Api.Dtos;

public record CategoryDto(Guid Id, string Name, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public static CategoryDto FromDomainModel(Category category)
        => new(category.Id.Value, category.Name, category.CreatedAt, category.UpdatedAt);
}

public record CreateCategoryDto(string Name);

public record UpdateCategoryDto(string Name);

public record CategoryFlowerDto(CategoryDto? Category)
{
    public static CategoryFlowerDto FromDomainModel(CategoryFlower categoryFlower)
        => new(categoryFlower.Category == null ? null : CategoryDto.FromDomainModel(categoryFlower.Category));
}