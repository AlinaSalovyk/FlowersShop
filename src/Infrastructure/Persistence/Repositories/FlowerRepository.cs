using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using Domain.Flowers;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class FlowerRepository(ApplicationDbContext context) : IFlowerRepository, IFlowerQueries
{
    
    public async Task<Option<Flower>> GetByIdAsync(FlowerId id, CancellationToken cancellationToken)
    {
        var entity = await context.Flowers
            .Include(x => x.Categories)!
            .ThenInclude(x => x.Category)
            .Include(x => x.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);

        return entity ?? Option<Flower>.None;
    }
    
    public async Task<IReadOnlyList<Flower>> GetByIdsAsync(IReadOnlyList<FlowerId> ids, CancellationToken cancellationToken)
    {
        return await context.Flowers
            .Include(x => x.Categories)!
            .ThenInclude(x => x.Category)
            .Include(x => x.Images)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<Flower>> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var entity = await context.Flowers
            .Include(x => x.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        return entity ?? Option<Flower>.None;
    }

    public async Task<Flower> AddAsync(Flower entity, CancellationToken cancellationToken)
    {
        await context.Flowers.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Flower> UpdateAsync(Flower entity, CancellationToken cancellationToken)
    {
        context.Flowers.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Flower> DeleteAsync(Flower entity, CancellationToken cancellationToken)
    {
        context.Flowers.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<Flower>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Flowers
            .Include(x => x.Categories)!
            .ThenInclude(x => x.Category)
            .Include(x => x.Images)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Flower>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var typedCategoryId = new CategoryId(categoryId);

        return await context.Flowers
            .Include(x => x.Categories)!
            .ThenInclude(x => x.Category)
            .Include(x => x.Images)
            .Where(x => x.Categories!.Any(c => c.CategoryId == typedCategoryId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}