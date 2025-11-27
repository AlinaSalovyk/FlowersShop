using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Flowers.Exceptions;
using Domain.Categories;
using Domain.Flowers;
using LanguageExt;
using MediatR;

namespace Application.Flowers.Commands;

public record UpdateFlowerCommand : IRequest<Either<FlowerException, Flower>>
{
    public required Guid FlowerId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required int StockQuantity { get; init; }
    public required IReadOnlyList<Guid> Categories { get; init; }
}

public class UpdateFlowerCommandHandler(
    IFlowerRepository flowerRepository,
    ICategoryRepository categoryRepository,
    ICategoryFlowerRepository categoryFlowerRepository,
    IApplicationDbContext dbContext) 
    : IRequestHandler<UpdateFlowerCommand, Either<FlowerException, Flower>>
{
    public async Task<Either<FlowerException, Flower>> Handle(
        UpdateFlowerCommand request,
        CancellationToken cancellationToken)
    {
        var flowerId = new FlowerId(request.FlowerId);
        var existingFlower = await flowerRepository.GetByIdAsync(flowerId, cancellationToken);

        return await existingFlower.MatchAsync(
            f => UpdateEntity(f, request, cancellationToken),
            () => Task.FromResult<Either<FlowerException, Flower>>(
                new FlowerNotFoundException(flowerId)));
    }

    private async Task<Either<FlowerException, Flower>> UpdateEntity(
        Flower flower,
        UpdateFlowerCommand request,
        CancellationToken cancellationToken)
    {
        using var transaction = await dbContext.BeginTransactionAsync(cancellationToken);

        try
        {
            var categoryIds = request.Categories.Select(x => new CategoryId(x)).ToList();
            var categories = await categoryRepository.GetByIdsAsync(categoryIds, cancellationToken);

            if (categories.Count != categoryIds.Count)
            {
                return new FlowerCategoriesNotFoundException(flower.Id);
            }
            
            flower.UpdateDetails(request.Name, request.Description, request.Price, request.StockQuantity);


            var existingCategories = await categoryFlowerRepository.GetByFlowerIdAsync(flower.Id, cancellationToken);
            await categoryFlowerRepository.RemoveRangeAsync(existingCategories, cancellationToken);
            
            var newCategoryFlowers = categories
                .Select(c => CategoryFlower.New(c.Id, flower.Id))
                .ToList();

            await categoryFlowerRepository.AddRangeAsync(newCategoryFlowers, cancellationToken);
            await flowerRepository.UpdateAsync(flower, cancellationToken);
            transaction.Commit();

            return flower;
        }
        catch (Exception exception)
        {
            return new UnhandledFlowerException(flower.Id, exception);
        }
    }
}