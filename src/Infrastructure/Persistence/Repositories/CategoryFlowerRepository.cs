using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using Domain.Flowers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CategoryFlowerRepository(ApplicationDbContext context) : ICategoryFlowerRepository
{
    public async Task<IReadOnlyList<CategoryFlower>> AddRangeAsync(
        IReadOnlyList<CategoryFlower> entities,
        CancellationToken cancellationToken)
    {
        await context.CategoryFlowers.AddRangeAsync(entities, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entities;
    }

    public async Task<IReadOnlyList<CategoryFlower>> RemoveRangeAsync(
        IReadOnlyList<CategoryFlower> entities,
        CancellationToken cancellationToken)
    {
        context.CategoryFlowers.RemoveRange(entities);
        await context.SaveChangesAsync(cancellationToken);
        return entities;
    }

    public async Task<IReadOnlyList<CategoryFlower>> GetByFlowerIdAsync(
        FlowerId flowerId,
        CancellationToken cancellationToken)
    {
        return await context.CategoryFlowers
            .AsNoTracking()
            .Where(x => x.FlowerId.Equals(flowerId))
            .ToListAsync(cancellationToken);
    }
}