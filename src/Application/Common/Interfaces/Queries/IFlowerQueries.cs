using Domain.Flowers;

namespace Application.Common.Interfaces.Queries;

public interface IFlowerQueries
{
    Task<IReadOnlyList<Flower>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Flower>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken);
}