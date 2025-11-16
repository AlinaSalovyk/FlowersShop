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
    private readonly Category _thirdTestCategory = CategoriesData.ThirdTestCategory();

    private const string BaseRoute = "api/categories";
    private readonly string _detailRoute;

    public CategoriesControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _detailRoute = $"{BaseRoute}/{_firstTestCategory.Id.Value}";
    }

    #region GET Tests

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
    public async Task ShouldGetAllCategories_WhenMultipleCategoriesExist()
    {
        // Arrange
        await Context.Categories.AddRangeAsync(_secondTestCategory, _thirdTestCategory);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.ToResponseModel<List<CategoryDto>>();
        categories.Should().HaveCount(3);
        categories.Should().Contain(c => c.Name == _firstTestCategory.Name);
        categories.Should().Contain(c => c.Name == _secondTestCategory.Name);
        categories.Should().Contain(c => c.Name == _thirdTestCategory.Name);
    }

    [Fact]
    public async Task ShouldGetAllCategories_WhenNoCategoriesExist()
    {
        // Arrange
        Context.Categories.Remove(_firstTestCategory);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.ToResponseModel<List<CategoryDto>>();
        categories.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetCategoryById()
    {
        // Act
        var response = await Client.GetAsync(_detailRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categoryDto = await response.ToResponseModel<CategoryDto>();
        categoryDto.Id.Should().Be(_firstTestCategory.Id.Value);
        categoryDto.Name.Should().Be(_firstTestCategory.Name);
        categoryDto.CreatedAt.Should().BeCloseTo(_firstTestCategory.CreatedAt, TimeSpan.FromSeconds(1));
        categoryDto.UpdatedAt.Should().Be(_firstTestCategory.UpdatedAt);
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenCategoryDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenGetByIdWithInvalidGuid()
    {
        // Arrange
        var route = $"{BaseRoute}/invalid-guid";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST (Create) Tests

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

        categoryDto.Name.Should().Be(_secondTestCategory.Name);
        categoryDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        categoryDto.UpdatedAt.Should().BeNull();

        var dbCategory = await Context.Categories.FirstAsync(x => x.Id.Equals(categoryId));
        dbCategory.Name.Should().Be(_secondTestCategory.Name);
        dbCategory.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dbCategory.UpdatedAt.Should().BeNull();

        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/categories/{categoryDto.Id}");
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

        // Verify database wasn't modified
        var categoriesCount = await Context.Categories.CountAsync();
        categoriesCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryWithEmptyName()
    {
        // Arrange
        var request = new CreateCategoryDto("");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryWithWhitespaceName()
    {
        // Arrange
        var request = new CreateCategoryDto("   ");

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryWithNullName()
    {
        // Arrange
        var request = new CreateCategoryDto(null!);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryWithTooLongName()
    {
        // Arrange
        var longName = new string('a', 256);
        var request = new CreateCategoryDto(longName);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldCreateCategoryWithMaximumAllowedNameLength()
    {
        // Arrange
        var maxLengthName = new string('a', 255);
        var request = new CreateCategoryDto(maxLengthName);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var categoryDto = await response.ToResponseModel<CategoryDto>();
        categoryDto.Name.Should().Be(maxLengthName);
    }
    

    #endregion

    #region PUT (Update) Tests

    [Fact]
    public async Task ShouldUpdateCategory()
    {
        // Arrange
        var request = new UpdateCategoryDto("Updated Category Name");

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryDto = await response.ToResponseModel<CategoryDto>();
        categoryDto.Name.Should().Be("Updated Category Name");
        categoryDto.UpdatedAt.Should().NotBeNull();
        categoryDto.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var dbCategory = await Context.Categories.FirstAsync(x => x.Id.Equals(_firstTestCategory.Id));
        dbCategory.Name.Should().Be("Updated Category Name");
        dbCategory.UpdatedAt.Should().NotBeNull();
        dbCategory.CreatedAt.Should().BeCloseTo(_firstTestCategory.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task ShouldNotUpdateNonExistentCategory()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";
        var request = new UpdateCategoryDto("Updated Name");

        // Act
        var response = await Client.PutAsJsonAsync(route, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotUpdateCategoryWithEmptyName()
    {
        // Arrange
        var request = new UpdateCategoryDto("");

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify database wasn't modified
        var dbCategory = await Context.Categories.FirstAsync(x => x.Id.Equals(_firstTestCategory.Id));
        dbCategory.Name.Should().Be(_firstTestCategory.Name);
    }

    [Fact]
    public async Task ShouldNotUpdateCategoryWithWhitespaceName()
    {
        // Arrange
        var request = new UpdateCategoryDto("   ");

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateCategoryWithNullName()
    {
        // Arrange
        var request = new UpdateCategoryDto(null!);

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateCategoryWithTooLongName()
    {
        // Arrange
        var longName = new string('b', 256);
        var request = new UpdateCategoryDto(longName);

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldUpdateCategoryWithMaximumAllowedNameLength()
    {
        // Arrange
        var maxLengthName = new string('b', 255);
        var request = new UpdateCategoryDto(maxLengthName);

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categoryDto = await response.ToResponseModel<CategoryDto>();
        categoryDto.Name.Should().Be(maxLengthName);
    }
    

    [Fact]
    public async Task ShouldUpdateCategoryWithSameName()
    {
        // Arrange
        var request = new UpdateCategoryDto(_firstTestCategory.Name);

        // Act
        var response = await Client.PutAsJsonAsync(_detailRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categoryDto = await response.ToResponseModel<CategoryDto>();
        categoryDto.Name.Should().Be(_firstTestCategory.Name);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenUpdateWithInvalidGuid()
    {
        // Arrange
        var route = $"{BaseRoute}/invalid-guid";
        var request = new UpdateCategoryDto("New Name");

        // Act
        var response = await Client.PutAsJsonAsync(route, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task ShouldDeleteCategory()
    {
        // Act
        var response = await Client.DeleteAsync(_detailRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categoryDto = await response.ToResponseModel<CategoryDto>();
        categoryDto.Id.Should().Be(_firstTestCategory.Id.Value);
        categoryDto.Name.Should().Be(_firstTestCategory.Name);

        var dbCategory = await Context.Categories.FirstOrDefaultAsync(x => x.Id.Equals(_firstTestCategory.Id));
        dbCategory.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotDeleteNonExistentCategory()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenDeleteWithInvalidGuid()
    {
        // Arrange
        var route = $"{BaseRoute}/invalid-guid";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotDeleteCategoryTwice()
    {
        // Arrange
        await Client.DeleteAsync(_detailRoute);

        // Act
        var response = await Client.DeleteAsync(_detailRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Initialization and Cleanup

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

    #endregion
}