using Claims.Contracts;
using Claims.Domain;

namespace Claims.Services;

/// <summary>Use cases for insurance claims.</summary>
public interface IClaimsService
{
    Task<IReadOnlyList<Claim>> GetAllAsync();

    /// <returns>The claim, or <c>null</c> if it does not exist.</returns>
    Task<Claim?> GetByIdAsync(string id);

    Task<Claim> CreateAsync(CreateClaimRequest request);

    /// <returns><c>false</c> if the claim does not exist.</returns>
    Task<bool> DeleteAsync(string id);
}
