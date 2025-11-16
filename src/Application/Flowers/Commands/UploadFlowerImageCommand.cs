using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Flowers.Exceptions;
using Domain.Flowers;
using LanguageExt;
using MediatR;

namespace Application.Flowers.Commands;

public record UploadFlowerImagesCommand : IRequest<Either<FlowerException, Flower>>
{
    public required Guid FlowerId { get; init; }
    public required IReadOnlyList<ImageFileDto> Images { get; init; }
}

public record ImageFileDto
{
    public required string OriginalName { get; init; }
    public required Stream FileStream { get; init; }
}

public class UploadFlowerImagesCommandHandler(
    IFlowerRepository flowerRepository,
    IFlowerImageRepository flowerImageRepository,
    IFileStorage fileStorage)
    : IRequestHandler<UploadFlowerImagesCommand, Either<FlowerException, Flower>>
{
    public async Task<Either<FlowerException, Flower>> Handle(
        UploadFlowerImagesCommand request,
        CancellationToken cancellationToken)
    {
        var flowerId = new FlowerId(request.FlowerId);
        var existingFlower = await flowerRepository.GetByIdAsync(flowerId, cancellationToken);

        return await existingFlower.MatchAsync(
            async flower => 
            {
                try
                {
                    var images = new List<FlowerImage>();
                    
                    foreach (var imageDto in request.Images)
                    {
                        var image = FlowerImage.New(flower.Id, imageDto.OriginalName);
                        images.Add(image);
                        await fileStorage.UploadAsync(imageDto.FileStream, image.GetFilePath(), cancellationToken);
                    }
                    
                    await flowerImageRepository.AddRangeAsync(images, cancellationToken);
                    
                    return (Either<FlowerException, Flower>)flower;
                }
                catch (Exception exception)
                {
                    return (Either<FlowerException, Flower>)new UnhandledFlowerException(flower.Id, exception);
                }
            },
            () => Task.FromResult<Either<FlowerException, Flower>>(
                new FlowerNotFoundException(flowerId)));
    }
}