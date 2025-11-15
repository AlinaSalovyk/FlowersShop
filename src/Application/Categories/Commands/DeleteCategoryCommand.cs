using Application.Categories.Exceptions;
using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using LanguageExt;
using MediatR;

namespace Application.Categories.Commands;

public record DeleteCategoryCommand : IRequest<Either<CategoryException, Category>>
{
    public required Guid CategoryId { get; init; }
}

public class DeleteCategoryCommandHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<DeleteCategoryCommand, Either<CategoryException, Category>>
{
    public async Task<Either<CategoryException, Category>> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);
        var existingCategory = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

        return await existingCategory.MatchAsync(
            c => DeleteEntity(c, cancellationToken),
            () => Task.FromResult<Either<CategoryException, Category>>(
                new CategoryNotFoundException(categoryId)));
    }

    private async Task<Either<CategoryException, Category>> DeleteEntity(
        Category category,
        CancellationToken cancellationToken)
    {
        try
        {
            return await categoryRepository.DeleteAsync(category, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledCategoryException(category.Id, exception);
        }
    }
}