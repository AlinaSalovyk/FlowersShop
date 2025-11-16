using Domain.Flowers;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IFlowerImageRepository
{
    Task<FlowerImage> AddAsync(FlowerImage entity, CancellationToken cancellationToken);
    Task<IReadOnlyList<FlowerImage>> AddRangeAsync(IReadOnlyList<FlowerImage> entities, CancellationToken cancellationToken);
    Task<Option<FlowerImage>> GetByIdAsync(FlowerImageId id, CancellationToken cancellationToken);
    Task<FlowerImage> DeleteAsync(FlowerImage entity, CancellationToken cancellationToken);
}