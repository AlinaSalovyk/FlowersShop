using Domain.Flowers;

namespace Application.Common.Interfaces.Repositories;

public interface IFlowerImageRepository
{
    Task<FlowerImage> AddAsync(FlowerImage entity, CancellationToken cancellationToken);
}