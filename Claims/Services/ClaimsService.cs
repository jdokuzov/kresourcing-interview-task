using Claims.Auditing;
using Claims.Contracts;
using Claims.Domain;
using Claims.Persistence;
using Claims.Validation;

namespace Claims.Services;

public sealed class ClaimsService(
    IRepository<Claim> claims,
    IValidator<CreateClaimRequest> validator,
    IAuditer auditer) : IClaimsService
{
    public Task<IReadOnlyList<Claim>> GetAllAsync() => claims.GetAllAsync();

    public Task<Claim?> GetByIdAsync(string id) => claims.GetByIdAsync(id);

    public async Task<Claim> CreateAsync(CreateClaimRequest request)
    {
        await validator.ThrowIfInvalidAsync(request);

        var claim = new Claim
        {
            Id = Guid.NewGuid().ToString(),
            CoverId = request.CoverId,
            Created = request.Created,
            Name = request.Name,
            Type = request.Type,
            DamageCost = request.DamageCost
        };

        await claims.AddAsync(claim);
        await auditer.AuditClaimAsync(claim.Id, HttpMethods.Post);
        return claim;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var deleted = await claims.DeleteAsync(id);
        if (deleted)
        {
            await auditer.AuditClaimAsync(id, HttpMethods.Delete);
        }

        return deleted;
    }
}
