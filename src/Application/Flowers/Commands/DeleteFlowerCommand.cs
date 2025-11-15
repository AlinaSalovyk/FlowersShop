using Application.Common.Interfaces.Repositories;
using Application.Flowers.Exceptions;
using Domain.Flowers;
using LanguageExt;
using MediatR;

namespace Application.Flowers.Commands;

public record DeleteFlowerCommand : IRequest<Either<FlowerException, Flower>>
{
    public required Guid FlowerId { get; init; }
}

public class DeleteFlowerCommandHandler(IFlowerRepository flowerRepository)
    : IRequestHandler<DeleteFlowerCommand, Either<FlowerException, Flower>>
{
    public async Task<Either<FlowerException, Flower>> Handle(
        DeleteFlowerCommand request,
        CancellationToken cancellationToken)
    {
        var flowerId = new FlowerId(request.FlowerId);
        var existingFlower = await flowerRepository.GetByIdAsync(flowerId, cancellationToken);

        return await existingFlower.MatchAsync(
            f => DeleteEntity(f, cancellationToken),
            () => Task.FromResult<Either<FlowerException, Flower>>(
                new FlowerNotFoundException(flowerId)));
    }

    private async Task<Either<FlowerException, Flower>> DeleteEntity(
        Flower flower,
        CancellationToken cancellationToken)
    {
        try
        {
            return await flowerRepository.DeleteAsync(flower, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledFlowerException(flower.Id, exception);
        }
    }
}