using Domain.Flowers;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IFlowerRepository
{
    Task<Flower> AddAsync(Flower entity, CancellationToken cancellationToken);
    Task<Flower> UpdateAsync(Flower entity, CancellationToken cancellationToken);
    Task<Flower> DeleteAsync(Flower entity, CancellationToken cancellationToken);
    Task<Option<Flower>> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<Option<Flower>> GetByIdAsync(FlowerId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Flower>> GetByIdsAsync(IReadOnlyList<FlowerId> ids, CancellationToken cancellationToken);
}