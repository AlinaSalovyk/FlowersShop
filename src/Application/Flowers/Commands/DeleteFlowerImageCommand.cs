using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Flowers.Exceptions;
using Domain.Flowers;
using LanguageExt;
using MediatR;

namespace Application.Flowers.Commands;

public record DeleteFlowerImageCommand : IRequest<Either<FlowerException, Flower>>
{
    public required Guid FlowerId { get; init; }
    public required Guid ImageId { get; init; }
}

public class DeleteFlowerImageCommandHandler(
    IFlowerRepository flowerRepository,
    IFlowerImageRepository flowerImageRepository,
    IFileStorage fileStorage)
    : IRequestHandler<DeleteFlowerImageCommand, Either<FlowerException, Flower>>
{
    public async Task<Either<FlowerException, Flower>> Handle(
        DeleteFlowerImageCommand request,
        CancellationToken cancellationToken)
    {
        var flowerId = new FlowerId(request.FlowerId);
        var imageId = new FlowerImageId(request.ImageId);
        
        var existingFlower = await flowerRepository.GetByIdAsync(flowerId, cancellationToken);

        return await existingFlower.MatchAsync(
            async flower => 
            {
                try
                {
                    var imageOption = await flowerImageRepository.GetByIdAsync(imageId, cancellationToken);
                    
                    return await imageOption.MatchAsync(
                        async image =>
                        {
                            // Видаляємо файл з файлової системи
                            await fileStorage.DeleteAsync(image.GetFilePath(), cancellationToken);
                            
                            // Видаляємо запис з бази даних
                            await flowerImageRepository.DeleteAsync(image, cancellationToken);
                            
                            // Оновлюємо квітку
                            flower.RemoveImage(imageId);
                            await flowerRepository.UpdateAsync(flower, cancellationToken);
                            
                            return (Either<FlowerException, Flower>)flower;
                        },
                        () => Task.FromResult<Either<FlowerException, Flower>>(
                            new FlowerImageNotFoundException(flowerId, imageId)));
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