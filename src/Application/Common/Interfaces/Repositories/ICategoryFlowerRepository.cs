using Domain.Categories;
using Domain.Flowers;

namespace Application.Common.Interfaces.Repositories;

public interface ICategoryFlowerRepository
{
    Task<IReadOnlyList<CategoryFlower>> AddRangeAsync(
        IReadOnlyList<CategoryFlower> entities,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryFlower>> RemoveRangeAsync(
        IReadOnlyList<CategoryFlower> entities,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryFlower>> GetByFlowerIdAsync(
        FlowerId flowerId,
        CancellationToken cancellationToken);
}