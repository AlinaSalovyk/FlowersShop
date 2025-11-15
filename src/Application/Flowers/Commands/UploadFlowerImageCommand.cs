using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Flowers.Exceptions;
using Domain.Flowers;
using LanguageExt;
using MediatR;

namespace Application.Flowers.Commands;

public record UploadFlowerImageCommand : IRequest<Either<FlowerException, Flower>>
{
    public required Guid FlowerId { get; init; }
    public required string OriginalName { get; init; }
    public required Stream FileStream { get; init; }
}

public class UploadFlowerImageCommandHandler(
    IFlowerRepository flowerRepository,
    IFlowerImageRepository flowerImageRepository,
    IFileStorage fileStorage)
    : IRequestHandler<UploadFlowerImageCommand, Either<FlowerException, Flower>>
{
    public async Task<Either<FlowerException, Flower>> Handle(
        UploadFlowerImageCommand request,
        CancellationToken cancellationToken)
    {
        var flowerId = new FlowerId(request.FlowerId);
        var existingFlower = await flowerRepository.GetByIdAsync(flowerId, cancellationToken);

        return await existingFlower.MatchAsync(
            async flower => 
            {
                try
                {
                    var image = FlowerImage.New(flower.Id, request.OriginalName);
                    await flowerImageRepository.AddAsync(image, cancellationToken);
                    await fileStorage.UploadAsync(request.FileStream, image.GetFilePath(), cancellationToken);
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