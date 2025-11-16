using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Api.Dtos;
using Domain.Flowers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Tests.Data.Flowers;
using Xunit;

namespace Api.Tests.Integration.Flowers;

public class FlowerImagesControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Flower _firstTestFlower = FlowersData.FirstTestFlower();
    private FlowerImage _firstTestFlowerImage = null!;

    private const string BaseRoute = "api/flowers";
    
    // 1. Додаємо поле для базового шляху до картинок цього конкретного квітки
    private readonly string _imagesRoute;

    public FlowerImagesControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        // 2. Формуємо шлях один раз: "api/flowers/{id}/images"
        _imagesRoute = $"{BaseRoute}/{_firstTestFlower.Id.Value}/images";
    }

    [Fact]
    public async Task ShouldUploadFlowerImages()
    {
        // Arrange
        // 3. Використовуємо готовий _imagesRoute
        using var content = new MultipartFormDataContent();
        
        // Create fake image files
        var imageContent1 = new ByteArrayContent(Encoding.UTF8.GetBytes("fake image content 1"));
        imageContent1.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent1, "files", "test-upload-1.jpg");

        var imageContent2 = new ByteArrayContent(Encoding.UTF8.GetBytes("fake image content 2"));
        imageContent2.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent2, "files", "test-upload-2.jpg");

        // Act
        var response = await Client.PostAsync(_imagesRoute, content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var flowerDto = await response.ToResponseModel<FlowerDto>();
        flowerDto.Id.Should().Be(_firstTestFlower.Id.Value);

        Context.ChangeTracker.Clear();
        var dbFlowerImages = await Context.FlowerImages
            .AsNoTracking()
            .Where(i => i.FlowerId == _firstTestFlower.Id)
            .ToListAsync();

        // Should have 3 images: 1 from initialization + 2 uploaded
        dbFlowerImages.Should().HaveCount(3);
        dbFlowerImages.Should().Contain(i => i.OriginalName == "test-image-1.jpg");
        dbFlowerImages.Should().Contain(i => i.OriginalName == "test-upload-1.jpg");
        dbFlowerImages.Should().Contain(i => i.OriginalName == "test-upload-2.jpg");
    }

    [Fact]
    public async Task ShouldUploadSingleFlowerImage()
    {
        // Arrange
        // 3. Знову _imagesRoute замість ручного складання рядка
        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake single image"));
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(imageContent, "files", "single-image.png");

        // Act
        var response = await Client.PostAsync(_imagesRoute, content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        Context.ChangeTracker.Clear();
        var dbFlowerImages = await Context.FlowerImages
            .AsNoTracking()
            .Where(i => i.FlowerId == _firstTestFlower.Id && i.OriginalName == "single-image.png")
            .ToListAsync();

        dbFlowerImages.Should().HaveCount(1);
        dbFlowerImages.First().OriginalName.Should().Be("single-image.png");
    }

    [Fact]
    public async Task ShouldNotUploadImagesForNonExistentFlower()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentId}/images";

        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake image content"));
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "files", "test-image.jpg");

        // Act
        var response = await Client.PostAsync(route, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotUploadWithoutFiles()
    {
        // Arrange
        using var content = new MultipartFormDataContent();

        // Act
        // 3. _imagesRoute
        var response = await Client.PostAsync(_imagesRoute, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldDeleteFlowerImage()
    {
        // Arrange
        // 3. Тут ми просто дописуємо ID картинки до базового _imagesRoute
        var route = $"{_imagesRoute}/{_firstTestFlowerImage.Id.Value}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var flowerDto = await response.ToResponseModel<FlowerDto>();
        flowerDto.Id.Should().Be(_firstTestFlower.Id.Value);

        Context.ChangeTracker.Clear();
        var dbImage = await Context.FlowerImages
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == _firstTestFlowerImage.Id);
        dbImage.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotDeleteNonExistentImage()
    {
        // Arrange
        var nonExistentImageId = Guid.NewGuid();
        // 3. Використання _imagesRoute для побудови шляху
        var route = $"{_imagesRoute}/{nonExistentImageId}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotDeleteImageForNonExistentFlower()
    {
        // Тут не можна використати _imagesRoute, бо ID квітки неправильний
        var nonExistentFlowerId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentFlowerId}/images/{_firstTestFlowerImage.Id.Value}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldDeleteMultipleImagesSequentially()
    {
        // Arrange - Upload 2 more images
        // 3. Використовуємо _imagesRoute для завантаження
        using var uploadContent = new MultipartFormDataContent();
        
        var img1 = new ByteArrayContent(Encoding.UTF8.GetBytes("image 1"));
        img1.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        uploadContent.Add(img1, "files", "delete-test-1.jpg");

        var img2 = new ByteArrayContent(Encoding.UTF8.GetBytes("image 2"));
        img2.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        uploadContent.Add(img2, "files", "delete-test-2.jpg");

        await Client.PostAsync(_imagesRoute, uploadContent);

        Context.ChangeTracker.Clear();
        var uploadedImages = await Context.FlowerImages
            .AsNoTracking()
            .Where(i => i.FlowerId == _firstTestFlower.Id)
            .OrderBy(i => i.OriginalName)
            .ToListAsync();

        uploadedImages.Should().HaveCountGreaterThanOrEqualTo(2);

        // Act - Delete the uploaded images
        foreach (var image in uploadedImages.Where(i => i.OriginalName.StartsWith("delete-test")))
        {
            // 3. Використовуємо _imagesRoute для формування шляху видалення
            var deleteRoute = $"{_imagesRoute}/{image.Id.Value}";
            var response = await Client.DeleteAsync(deleteRoute);
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        // Assert
        Context.ChangeTracker.Clear();
        var remainingImages = await Context.FlowerImages
            .AsNoTracking()
            .Where(i => i.FlowerId == _firstTestFlower.Id && i.OriginalName.StartsWith("delete-test"))
            .ToListAsync();

        remainingImages.Should().BeEmpty();
    }

    public async Task InitializeAsync()
    {
        var category = CategoriesData.FirstTestCategory();

        await Context.Categories.AddAsync(category);
        await Context.Flowers.AddAsync(_firstTestFlower);
        await SaveChangesAsync();

        _firstTestFlowerImage = FlowersData.FirstTestFlowerImage(_firstTestFlower.Id);
        await Context.FlowerImages.AddAsync(_firstTestFlowerImage);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.FlowerImages.RemoveRange(Context.FlowerImages);
        Context.Flowers.RemoveRange(Context.Flowers);
        Context.Categories.RemoveRange(Context.Categories);
        await SaveChangesAsync();
    }
}