using Domain.Categories;
using Domain.Flowers;

namespace Tests.Data.Categories;

public static class CategoriesData
{
    public static Category FirstTestCategory() => Category.New(CategoryId.New(), "Roses");
    public static Category SecondTestCategory() => Category.New(CategoryId.New(), "Tulips");
    public static Category ThirdTestCategory() => Category.New(CategoryId.New(), "Orchids");

    public static CategoryFlower FirstTestCategoryFlower(CategoryId categoryId, FlowerId flowerId)
        => CategoryFlower.New(categoryId, flowerId);
}