using Application.Categories.Exceptions;
using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using LanguageExt;
using MediatR;

namespace Application.Categories.Commands;

public record UpdateCategoryCommand : IRequest<Either<CategoryException, Category>>
{
    public required Guid CategoryId { get; init; }
    public required string Name { get; init; }
}

public class UpdateCategoryCommandHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<UpdateCategoryCommand, Either<CategoryException, Category>>
{
    public async Task<Either<CategoryException, Category>> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);
        var existingCategory = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

        return await existingCategory.MatchAsync(
            c => UpdateEntity(c, request, cancellationToken),
            () => Task.FromResult<Either<CategoryException, Category>>(
                new CategoryNotFoundException(categoryId)));
    }

    private async Task<Either<CategoryException, Category>> UpdateEntity(
        Category category,
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            category.UpdateDetails(request.Name);
            return await categoryRepository.UpdateAsync(category, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledCategoryException(category.Id, exception);
        }
    }
}