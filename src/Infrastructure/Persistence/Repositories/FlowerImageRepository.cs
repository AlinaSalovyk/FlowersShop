using Application.Common.Interfaces.Repositories;
using Domain.Flowers;

namespace Infrastructure.Persistence.Repositories;

public class FlowerImageRepository(ApplicationDbContext context) : IFlowerImageRepository
{
    public async Task<FlowerImage> AddAsync(FlowerImage entity, CancellationToken cancellationToken)
    {
        await context.FlowerImages.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }
}