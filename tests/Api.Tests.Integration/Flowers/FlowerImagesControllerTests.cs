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

namespace Api.Tests.Integration.Flowers;

public class FlowerImagesControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Flower _firstTestFlower = FlowersData.FirstTestFlower();
    private FlowerImage _firstTestFlowerImage = null!;

    private const string BaseRoute = "api/flowers";
    
    private readonly string _imagesRoute;

    public FlowerImagesControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _imagesRoute = $"{BaseRoute}/{_firstTestFlower.Id.Value}/images";
    }

    #region POST (Upload) Tests

    [Fact]
    public async Task ShouldUploadFlowerImages()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        
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

        dbFlowerImages.Should().HaveCount(3);
        dbFlowerImages.Should().Contain(i => i.OriginalName == "test-image-1.jpg");
        dbFlowerImages.Should().Contain(i => i.OriginalName == "test-upload-1.jpg");
        dbFlowerImages.Should().Contain(i => i.OriginalName == "test-upload-2.jpg");
    }

    [Fact]
    public async Task ShouldUploadSingleFlowerImage()
    {
        // Arrange
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
    public async Task ShouldUploadMultipleImagesWithDifferentFormats()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        
        var jpegContent = new ByteArrayContent(Encoding.UTF8.GetBytes("jpeg image"));
        jpegContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(jpegContent, "files", "image.jpg");

        var pngContent = new ByteArrayContent(Encoding.UTF8.GetBytes("png image"));
        pngContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(pngContent, "files", "image.png");

        var gifContent = new ByteArrayContent(Encoding.UTF8.GetBytes("gif image"));
        gifContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/gif");
        content.Add(gifContent, "files", "image.gif");

        // Act
        var response = await Client.PostAsync(_imagesRoute, content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        Context.ChangeTracker.Clear();
        var dbFlowerImages = await Context.FlowerImages
            .AsNoTracking()
            .Where(i => i.FlowerId == _firstTestFlower.Id)
            .ToListAsync();

        dbFlowerImages.Should().HaveCountGreaterThanOrEqualTo(3);
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
        var response = await Client.PostAsync(_imagesRoute, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUploadImagesWithEmptyFlowerId()
    {
        // Arrange
        var route = $"{BaseRoute}/{Guid.Empty}/images";
        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake image"));
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "files", "test.jpg");

        // Act
        var response = await Client.PostAsync(route, content);

        // Assert
        // Виправлено з NotFound на BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldUploadImageWithLongFileName()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        var longFileName = new string('a', 200) + ".jpg";
        var imageContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake image"));
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "files", longFileName);

        // Act
        var response = await Client.PostAsync(_imagesRoute, content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldUploadImageWithSpecialCharactersInFileName()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake image"));
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "files", "test-image-@#$%^.jpg");

        // Act
        var response = await Client.PostAsync(_imagesRoute, content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    #endregion

    #region DELETE Image Tests

    [Fact]
    public async Task ShouldDeleteFlowerImage()
    {
        // Arrange
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
        var route = $"{_imagesRoute}/{nonExistentImageId}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotDeleteImageForNonExistentFlower()
    {
        // Arrange
        var nonExistentFlowerId = Guid.NewGuid();
        var route = $"{BaseRoute}/{nonExistentFlowerId}/images/{_firstTestFlowerImage.Id.Value}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotDeleteImageWithEmptyImageId()
    {
        // Arrange
        var route = $"{_imagesRoute}/{Guid.Empty}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotDeleteImageWithEmptyFlowerId()
    {
        // Arrange
        var route = $"{BaseRoute}/{Guid.Empty}/images/{_firstTestFlowerImage.Id.Value}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldDeleteMultipleImagesSequentially()
    {
        // Arrange
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

        // Act 
        foreach (var image in uploadedImages.Where(i => i.OriginalName.StartsWith("delete-test")))
        {
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

    [Fact]
    public async Task ShouldDeleteAllImagesOfFlower()
    {
        // Arrange 
        using var uploadContent = new MultipartFormDataContent();
        
        var img1 = new ByteArrayContent(Encoding.UTF8.GetBytes("image 1"));
        img1.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        uploadContent.Add(img1, "files", "temp-1.jpg");

        var img2 = new ByteArrayContent(Encoding.UTF8.GetBytes("image 2"));
        img2.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        uploadContent.Add(img2, "files", "temp-2.jpg");

        await Client.PostAsync(_imagesRoute, uploadContent);

        Context.ChangeTracker.Clear();
        var allImages = await Context.FlowerImages
            .AsNoTracking()
            .Where(i => i.FlowerId == _firstTestFlower.Id)
            .ToListAsync();

        // Act 
        foreach (var image in allImages)
        {
            var deleteRoute = $"{_imagesRoute}/{image.Id.Value}";
            await Client.DeleteAsync(deleteRoute);
        }

        // Assert
        Context.ChangeTracker.Clear();
        var remainingImages = await Context.FlowerImages
            .AsNoTracking()
            .Where(i => i.FlowerId == _firstTestFlower.Id)
            .ToListAsync();

        remainingImages.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotDeleteImageFromDifferentFlower()
    {
        // Arrange 
        var secondFlower = FlowersData.SecondTestFlower();
        await Context.Flowers.AddAsync(secondFlower);
        await SaveChangesAsync();

        var secondFlowerImage = FlowersData.FirstTestFlowerImage(secondFlower.Id);
        await Context.FlowerImages.AddAsync(secondFlowerImage);
        await SaveChangesAsync();
        var route = $"{_imagesRoute}/{secondFlowerImage.Id.Value}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        Context.ChangeTracker.Clear();
        var dbImage = await Context.FlowerImages
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == secondFlowerImage.Id);
        dbImage.Should().NotBeNull();
    }

    #endregion

    #region Integration Tests (Upload + Delete)

    [Fact]
    public async Task ShouldUploadAndThenDeleteImage()
    {
        // Arrange & Act - Upload
        using var uploadContent = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test image"));
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        uploadContent.Add(imageContent, "files", "upload-delete-test.jpg");

        var uploadResponse = await Client.PostAsync(_imagesRoute, uploadContent);
        uploadResponse.IsSuccessStatusCode.Should().BeTrue();

        Context.ChangeTracker.Clear();
        var uploadedImage = await Context.FlowerImages
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.FlowerId == _firstTestFlower.Id && i.OriginalName == "upload-delete-test.jpg");
        uploadedImage.Should().NotBeNull();

        // Act 
        var deleteRoute = $"{_imagesRoute}/{uploadedImage.Id.Value}";
        var deleteResponse = await Client.DeleteAsync(deleteRoute);

        // Assert
        deleteResponse.IsSuccessStatusCode.Should().BeTrue();

        Context.ChangeTracker.Clear();
        var deletedImage = await Context.FlowerImages
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == uploadedImage.Id);
        deletedImage.Should().BeNull();
    }

    [Fact]
    public async Task ShouldUploadMultipleImagesAndDeleteSomeOfThem()
    {
        // Arrange & Act - Upload
        using var uploadContent = new MultipartFormDataContent();
        
        for (int i = 1; i <= 5; i++)
        {
            var imgContent = new ByteArrayContent(Encoding.UTF8.GetBytes($"image {i}"));
            imgContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            uploadContent.Add(imgContent, "files", $"batch-test-{i}.jpg");
        }

        await Client.PostAsync(_imagesRoute, uploadContent);

        Context.ChangeTracker.Clear();
        var uploadedImages = await Context.FlowerImages
            .AsNoTracking()
            .Where(i => i.FlowerId == _firstTestFlower.Id && i.OriginalName.StartsWith("batch-test"))
            .ToListAsync();

        uploadedImages.Should().HaveCount(5);

        // Act - Delete first 3 images
        var imagesToDelete = uploadedImages.Take(3).ToList();
        foreach (var image in imagesToDelete)
        {
            var deleteRoute = $"{_imagesRoute}/{image.Id.Value}";
            var deleteResponse = await Client.DeleteAsync(deleteRoute);
            deleteResponse.IsSuccessStatusCode.Should().BeTrue();
        }

        // Assert
        Context.ChangeTracker.Clear();
        var remainingImages = await Context.FlowerImages
            .AsNoTracking()
            .Where(i => i.FlowerId == _firstTestFlower.Id && i.OriginalName.StartsWith("batch-test"))
            .ToListAsync();

        remainingImages.Should().HaveCount(2);
    }

    #endregion

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