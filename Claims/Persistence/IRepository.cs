namespace Claims.Persistence;

/// <summary>
/// Storage operations for an entity identified by a string id.
/// Additions and removals take effect on <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync();

    /// <returns>The entity, or <c>null</c> if it does not exist.</returns>
    Task<T?> GetByIdAsync(string id);

    void Add(T entity);

    /// <returns><c>false</c> if the entity does not exist.</returns>
    Task<bool> RemoveAsync(string id);
}
