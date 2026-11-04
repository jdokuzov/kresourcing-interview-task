using Claims.Contracts;
using Claims.Domain;

namespace Claims.Services;

/// <summary>Use cases for insurance covers.</summary>
public interface ICoversService
{
    Task<IReadOnlyList<Cover>> GetAllAsync();

    /// <returns>The cover, or <c>null</c> if it does not exist.</returns>
    Task<Cover?> GetByIdAsync(string id);

    Task<Cover> CreateAsync(CreateCoverRequest request);

    /// <returns><c>false</c> if the cover does not exist.</returns>
    Task<bool> DeleteAsync(string id);

    decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType);
}
