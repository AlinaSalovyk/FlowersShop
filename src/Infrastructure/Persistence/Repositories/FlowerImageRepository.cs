using Application.Common.Interfaces.Repositories;
using Domain.Flowers;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class FlowerImageRepository(ApplicationDbContext context) : IFlowerImageRepository
{
    public async Task<FlowerImage> AddAsync(FlowerImage entity, CancellationToken cancellationToken)
    {
        await context.FlowerImages.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<FlowerImage>> AddRangeAsync(
        IReadOnlyList<FlowerImage> entities,
        CancellationToken cancellationToken)
    {
        await context.FlowerImages.AddRangeAsync(entities, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entities;
    }

    public async Task<Option<FlowerImage>> GetByIdAsync(FlowerImageId id, CancellationToken cancellationToken)
    {
        var entity = await context.FlowerImages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<FlowerImage>.None;
    }

    public async Task<FlowerImage> DeleteAsync(FlowerImage entity, CancellationToken cancellationToken)
    {
        context.FlowerImages.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }
}