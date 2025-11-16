using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Flowers;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class FlowerRepository : IFlowerRepository, IFlowerQueries
{
    private readonly ApplicationDbContext _context;

    public FlowerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Option<Flower>> GetByIdAsync(FlowerId id, CancellationToken cancellationToken)
    {
        var entity = await _context.Flowers
            .Include(x => x.Categories)!
            .ThenInclude(x => x.Category)
            .Include(x => x.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);

        return entity ?? Option<Flower>.None;
    }

    public async Task<Option<Flower>> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var entity = await _context.Flowers
            .Include(x => x.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        return entity ?? Option<Flower>.None;
    }

    public async Task<Flower> AddAsync(Flower entity, CancellationToken cancellationToken)
    {
        await _context.Flowers.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Flower> UpdateAsync(Flower entity, CancellationToken cancellationToken)
    {
        _context.Flowers.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Flower> DeleteAsync(Flower entity, CancellationToken cancellationToken)
    {
        _context.Flowers.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<Flower>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Flowers
            .Include(x => x.Categories)!
            .ThenInclude(x => x.Category)
            .Include(x => x.Images)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Flower>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return await _context.Flowers
            .Include(x => x.Categories)!
            .ThenInclude(x => x.Category)
            .Include(x => x.Images)
            .Where(x => x.Categories!.Any(c => c.CategoryId.Value == categoryId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}