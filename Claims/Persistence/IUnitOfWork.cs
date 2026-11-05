namespace Claims.Persistence;

/// <summary>Saves everything added or removed through repositories in one transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
