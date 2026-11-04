namespace Claims.Persistence;

/// <summary>Storage operations for an entity identified by a string id.</summary>
public interface IRepository<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync();

    /// <returns>The entity, or <c>null</c> if it does not exist.</returns>
    Task<T?> GetByIdAsync(string id);

    Task AddAsync(T entity);

    /// <returns><c>false</c> if the entity does not exist.</returns>
    Task<bool> DeleteAsync(string id);
}
