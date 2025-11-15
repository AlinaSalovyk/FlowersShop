using Application.Common.Interfaces.Repositories;
using Application.Flowers.Exceptions;
using Domain.Categories;
using Domain.Flowers;
using LanguageExt;
using MediatR;

namespace Application.Flowers.Commands;

public record CreateFlowerCommand : IRequest<Either<FlowerException, Flower>>
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required int StockQuantity { get; init; }
    public required IReadOnlyList<Guid> Categories { get; init; }
}

public class CreateFlowerCommandHandler(
    IFlowerRepository flowerRepository,
    ICategoryRepository categoryRepository)
    : IRequestHandler<CreateFlowerCommand, Either<FlowerException, Flower>>
{
    public async Task<Either<FlowerException, Flower>> Handle(
        CreateFlowerCommand request,
        CancellationToken cancellationToken)
    {
        var existingFlower = await flowerRepository.GetByNameAsync(request.Name, cancellationToken);

        return await existingFlower.MatchAsync(
            f => new FlowerAlreadyExistException(f.Id),
            async () => await CreateEntity(request, cancellationToken));
    }

    private async Task<Either<FlowerException, Flower>> CreateEntity(
        CreateFlowerCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var flowerId = FlowerId.New();
            var categoryIds = request.Categories.Select(x => new CategoryId(x)).ToList();
            var categories = await categoryRepository.GetByIdsAsync(categoryIds, cancellationToken);

            if (categories.Count != categoryIds.Count)
            {
                return new FlowerCategoriesNotFoundException(flowerId);
            }

            var categoryFlowers = categories
                .Select(c => CategoryFlower.New(c.Id, flowerId))
                .ToList();

            var flower = await flowerRepository.AddAsync(
                Flower.New(flowerId, request.Name, request.Description, request.Price, request.StockQuantity, categoryFlowers),
                cancellationToken);

            return flower;
        }
        catch (Exception exception)
        {
            return new UnhandledFlowerException(FlowerId.Empty(), exception);
        }
    }
}