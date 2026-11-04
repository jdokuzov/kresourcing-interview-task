using Microsoft.EntityFrameworkCore;

namespace Claims.Persistence;

/// <summary>Entity Framework implementation of <see cref="IRepository{T}"/>.</summary>
public sealed class Repository<T>(ClaimsContext context) : IRepository<T> where T : class
{
    public async Task<IReadOnlyList<T>> GetAllAsync() =>
        await context.Set<T>().AsNoTracking().ToListAsync();

    public async Task<T?> GetByIdAsync(string id) =>
        await context.Set<T>().FindAsync(id);

    public async Task AddAsync(T entity)
    {
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is null)
        {
            return false;
        }

        context.Set<T>().Remove(entity);
        await context.SaveChangesAsync();
        return true;
    }
}
