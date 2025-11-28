using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Categories;
using Domain.Flowers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Tests.Data.Flowers;

namespace Api.Tests.Integration.Flowers;

public class FlowersControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Category _firstTestCategory = CategoriesData.FirstTestCategory();
    private readonly Category _secondTestCategory = CategoriesData.SecondTestCategory();

    private readonly Flower _firstTestFlower = FlowersData.FirstTestFlower();
    private readonly Flower _secondTestFlower = FlowersData.SecondTestFlower();

    private readonly CategoryFlower _firstTestCategoryFlower;

    private const string BaseRoute = "api/flowers";
    
    private readonly string _detailRoute;
    private readonly string _categoryRoute;

    public FlowersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _firstTestCategoryFlower = CategoriesData.FirstTestCategoryFlower(
            _firstTestCategory.Id,
            _firstTestFlower.Id);

        _detailRoute = $"{BaseRoute}/{_firstTestFlower.Id.Value}";
        _categoryRoute = $"{BaseRoute}/category/{_firstTestCategory.Id.Value}";
    }

    #region GET Tests

    [Fact]
    public async Task ShouldGetAllFlowers()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var flowers = await response.ToResponseModel<List<FlowerDto>>();
        flowers.Should().HaveCount(1);
        flowers.First().Name.Should().Be(_firstTestFlower.Name);
        flowers.First().Description.Should().Be(_firstTestFlower.Description);
        flowers.First().Price.Should().Be(_firstTestFlower.Price);
        flowers.First().StockQuantity.Should().Be(_firstTestFlower.StockQuantity);
    }

    [Fact]
    public async Task ShouldGetEmptyListWhenNoFlowers()
    {
        // Arrange
        Context.CategoryFlowers.RemoveRange(Context.CategoryFlowers);
        Context.Flowers.RemoveRange(Context.Flowers);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var flowers = await response.ToResponseModel<List<FlowerDto>>();
        flowers.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetFlowersByCategory()
    {
        // Act
        var response = await Client.GetAsync(_categoryRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var flowers = await response.ToResponseModel<List<FlowerDto>>();
        flowers.Should().HaveCount(1);
        flowers.First().Name.Should().Be(_firstTestFlower.Name);
    }

    [Fact]
    public async Task ShouldGetEmptyListForCategoryWithoutFlowers()
    {
        // Arrange
        var emptyCategoryRoute = $"{BaseRoute}/category/{_secondTestCategory.Id.Value}";

        // Act
        var response = await Client.GetAsync(emptyCategoryRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var flowers = await response.ToResponseModel<List<FlowerDto>>();
        flowers.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetEmptyListForNonExistentCategory()
    {
        // Arrange
        var nonExistentCategoryRoute = $"{BaseRoute}/category/{Guid.NewGuid()}";

        // Act
        var response = await Client.GetAsync(nonExistentCategoryRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var flowers = await response.ToResponseModel<List<FlowerDto>>();
        flowers.Should().BeEmpty();
    }

    #endregion

    #region POST (Create) Tests

    [Fact]
    public async Task ShouldCreateFlower()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: _secondTestFlower.Name,
            Description: _secondTestFlower.Description,
            Price: _secondTestFlower.Price,
            StockQuantity: _secondTestFlower.StockQuantity,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var flowerDto = await response.ToResponseModel<FlowerDto>();
        var flowerId = new FlowerId(flowerDto.Id);

        var dbFlower = await Context.Flowers
            .Include(f => f.Categories)
            .FirstAsync(x => x.Id.Equals(flowerId));

        dbFlower.Name.Should().Be(_secondTestFlower.Name);
        dbFlower.Description.Should().Be(_secondTestFlower.Description);
        dbFlower.Price.Should().Be(_secondTestFlower.Price);
        dbFlower.StockQuantity.Should().Be(_secondTestFlower.StockQuantity);
        dbFlower.Categories.Should().HaveCount(1);
    }

    [Fact]
    public async Task ShouldCreateFlowerWithMultipleCategories()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: _secondTestFlower.Name,
            Description: _secondTestFlower.Description,
            Price: _secondTestFlower.Price,
            StockQuantity: _secondTestFlower.StockQuantity,
            Categories: [_firstTestCategory.Id.Value, _secondTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var flowerDto = await response.ToResponseModel<FlowerDto>();
        var flowerId = new FlowerId(flowerDto.Id);

        var dbFlower = await Context.Flowers
            .Include(f => f.Categories)
            .FirstAsync(x => x.Id.Equals(flowerId));

        dbFlower.Categories.Should().HaveCount(2);
    }

    [Fact]
    public async Task ShouldNotCreateFlowerBecauseNameDuplication()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: _firstTestFlower.Name,
            Description: "Some description",
            Price: 10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShouldNotCreateFlowerWithNonExistentCategory()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: "New Flower",
            Description: "Description",
            Price: 15.00m,
            StockQuantity: 20,
            Categories: [Guid.NewGuid()]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotCreateFlowerWithInvalidData()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: "",
            Description: "",
            Price: -10.00m,
            StockQuantity: -5,
            Categories: []);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateFlowerWithEmptyName()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: "",
            Description: "Valid description",
            Price: 10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateFlowerWithEmptyDescription()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: "Valid Name",
            Description: "",
            Price: 10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateFlowerWithNegativePrice()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: "Valid Name",
            Description: "Valid description",
            Price: -10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateFlowerWithZeroPrice()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: "Valid Name",
            Description: "Valid description",
            Price: 0m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateFlowerWithNegativeStockQuantity()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: "Valid Name",
            Description: "Valid description",
            Price: 10.00m,
            StockQuantity: -5,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldCreateFlowerWithZeroStockQuantity()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: _secondTestFlower.Name,
            Description: _secondTestFlower.Description,
            Price: _secondTestFlower.Price,
            StockQuantity: 0,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotCreateFlowerWithEmptyCategories()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: "Valid Name",
            Description: "Valid description",
            Price: 10.00m,
            StockQuantity: 10,
            Categories: []);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateFlowerWithTooLongName()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: new string('a', 256),
            Description: "Valid description",
            Price: 10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateFlowerWithTooLongDescription()
    {
        // Arrange
        var request = new CreateFlowerDto(
            Name: "Valid Name",
            Description: new string('a', 1001),
            Price: 10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT (Update) Tests

    [Fact]
    public async Task ShouldUpdateFlower()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: "Updated Flower Name",
            Description: "Updated description",
            Price: 25.99m,
            StockQuantity: 75,
            Categories: [_secondTestCategory.Id.Value]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var flowerDto = await response.ToResponseModel<FlowerDto>();
        flowerDto.Name.Should().Be("Updated Flower Name");
        flowerDto.Price.Should().Be(25.99m);

        var dbFlower = await Context.Flowers
            .Include(f => f.Categories)
            .AsNoTracking()
            .FirstAsync(x => x.Id.Equals(_firstTestFlower.Id));

        dbFlower.Name.Should().Be("Updated Flower Name");
        dbFlower.Description.Should().Be("Updated description");
        dbFlower.Price.Should().Be(25.99m);
        dbFlower.StockQuantity.Should().Be(75);

        var assignedCategories = await Context.CategoryFlowers
            .AsNoTracking()
            .Where(x => x.FlowerId.Equals(_firstTestFlower.Id))
            .ToListAsync();

        assignedCategories.Should().HaveCount(1);
        assignedCategories.First().CategoryId.Should().Be(_secondTestCategory.Id);
    }
    
    [Fact]
    public async Task ShouldNotUpdateNonExistentFlower()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: Guid.NewGuid(),
            Name: "Updated Name",
            Description: "Updated description",
            Price: 20.00m,
            StockQuantity: 50,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotUpdateFlowerWithNonExistentCategory()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: "Updated Name",
            Description: "Updated description",
            Price: 20.00m,
            StockQuantity: 50,
            Categories: [Guid.NewGuid()]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotUpdateFlowerWithInvalidData()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: "",
            Description: "",
            Price: -10.00m,
            StockQuantity: -5,
            Categories: []);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateFlowerWithEmptyName()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: "",
            Description: "Valid description",
            Price: 10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateFlowerWithEmptyDescription()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: "Valid Name",
            Description: "",
            Price: 10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateFlowerWithNegativePrice()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: "Valid Name",
            Description: "Valid description",
            Price: -10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateFlowerWithZeroPrice()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: "Valid Name",
            Description: "Valid description",
            Price: 0m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateFlowerWithNegativeStockQuantity()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: "Valid Name",
            Description: "Valid description",
            Price: 10.00m,
            StockQuantity: -5,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    

    [Fact]
    public async Task ShouldNotUpdateFlowerWithEmptyCategories()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: "Valid Name",
            Description: "Valid description",
            Price: 10.00m,
            StockQuantity: 10,
            Categories: []);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateFlowerWithTooLongName()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: new string('a', 256),
            Description: "Valid description",
            Price: 10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateFlowerWithTooLongDescription()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: _firstTestFlower.Id.Value,
            Name: "Valid Name",
            Description: new string('a', 1001),
            Price: 10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateFlowerWithEmptyId()
    {
        // Arrange
        var request = new UpdateFlowerDto(
            Id: Guid.Empty,
            Name: "Valid Name",
            Description: "Valid description",
            Price: 10.00m,
            StockQuantity: 10,
            Categories: [_firstTestCategory.Id.Value]);

        // Act
        var response = await Client.PutAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task ShouldDeleteFlower()
    {
        // Act
        var response = await Client.DeleteAsync(_detailRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var flowerDto = await response.ToResponseModel<FlowerDto>();
        flowerDto.Id.Should().Be(_firstTestFlower.Id.Value);

        var dbFlower = await Context.Flowers.FirstOrDefaultAsync(x => x.Id.Equals(_firstTestFlower.Id));
        dbFlower.Should().BeNull();
    }

    [Fact]
    public async Task ShouldDeleteFlowerAndItsRelations()
    {
        // Act
        var response = await Client.DeleteAsync(_detailRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var dbCategoryFlowers = await Context.CategoryFlowers
            .Where(x => x.FlowerId.Equals(_firstTestFlower.Id))
            .ToListAsync();
        dbCategoryFlowers.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotDeleteNonExistentFlower()
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
    public async Task ShouldNotDeleteFlowerWithEmptyId()
    {
        // Arrange
        var route = $"{BaseRoute}/{Guid.Empty}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    public async Task InitializeAsync()
    {
        await Context.Categories.AddAsync(_firstTestCategory);
        await Context.Categories.AddAsync(_secondTestCategory);
        await Context.Flowers.AddAsync(_firstTestFlower);
        await Context.CategoryFlowers.AddAsync(_firstTestCategoryFlower);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.CategoryFlowers.RemoveRange(Context.CategoryFlowers);
        Context.Flowers.RemoveRange(Context.Flowers);
        Context.Categories.RemoveRange(Context.Categories);
        await SaveChangesAsync();
    }
}