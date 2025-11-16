using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Categories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;

namespace Api.Tests.Integration.Categories;

public class CategoriesControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Category _firstTestCategory = CategoriesData.FirstTestCategory();
    private readonly Category _secondTestCategory = CategoriesData.SecondTestCategory();

    private const string BaseRoute = "api/categories";
    
    // 1. Додаємо поле для маршруту, який буде використовуватись у багатьох тестах
    private readonly string _detailRoute;

    public CategoriesControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        // 2. Тепер конструктор не пустий, він готує дані для тестів
        _detailRoute = $"{BaseRoute}/{_firstTestCategory.Id.Value}";
    }

    [Fact]
    public async Task ShouldGetAllCategories()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.ToResponseModel<List<CategoryDto>>();
        categories.Should().HaveCount(1);
        categories.First().Name.Should().Be(_firstTestCategory.Name);
    }

    [Fact]
    public async Task ShouldGetCategoryById()
    {
        // Act
        // 3. Використовуємо готовий _detailRoute замість створення змінної route
        var response = await Client.GetAsync(_detailRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categoryDto = await response.ToResponseModel<CategoryDto>();
        categoryDto.Id.Should().Be(_firstTestCategory.Id.Value);
        categoryDto.Name.Should().Be(_firstTestCategory.Name);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenCategoryDoesNotExist()
    {
        // Тут ми не можемо використати _detailRoute, бо нам потрібен неіснуючий ID
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldCreateCategory()
    {
        // Arrange
        var request = new CreateCategoryDto(_secondTestCategory.Name);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var categoryDto = await response.ToResponseModel<CategoryDto>();
        var categoryId = new CategoryId(categoryDto.Id);

        var dbCategory = await Context.Categories.FirstAsync(x => x.Id.Equals(categoryId));
        dbCategory.Name.Should().Be(_secondTestCategory.Name);
        dbCategory.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dbCategory.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotCreateCategoryBecauseNameDuplication()
    {
        // Arrange
        var request = new CreateCategoryDto(_firstTestCategory.Name);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryWithEmptyName()
    {
        var request = new CreateCategoryDto("");
        var response = await Client.PostAsJsonAsync(BaseRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryWithTooLongName()
    {
        var longName = new string('a', 256);
        var request = new CreateCategoryDto(longName);
        var response = await Client.PostAsJsonAsync(BaseRoute, request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldUpdateCategory()
    {
        // Arrange
        var request = new UpdateCategoryDto("Updated Category Name");

        // Act
        // 3. Знову використовуємо готовий _detailRoute
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var categoryDto = await response.ToResponseModel<CategoryDto>();
        categoryDto.Name.Should().Be("Updated Category Name");
        categoryDto.UpdatedAt.Should().NotBeNull();

        var dbCategory = await Context.Categories.FirstAsync(x => x.Id.Equals(_firstTestCategory.Id));
        dbCategory.Name.Should().Be("Updated Category Name");
    }

    [Fact]
    public async Task ShouldNotUpdateNonExistentCategory()
    {
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";
        var request = new UpdateCategoryDto("Updated Name");

        var response = await Client.PutAsJsonAsync(route, request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldDeleteCategory()
    {
        // Act
        // 3. І тут використовуємо _detailRoute
        var response = await Client.DeleteAsync(_detailRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var categoryDto = await response.ToResponseModel<CategoryDto>();
        categoryDto.Id.Should().Be(_firstTestCategory.Id.Value);

        var dbCategory = await Context.Categories.FirstOrDefaultAsync(x => x.Id.Equals(_firstTestCategory.Id));
        dbCategory.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotDeleteNonExistentCategory()
    {
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";

        var response = await Client.DeleteAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public async Task InitializeAsync()
    {
        await Context.Categories.AddAsync(_firstTestCategory);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.Categories.RemoveRange(Context.Categories);
        await SaveChangesAsync();
    }
}