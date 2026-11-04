using Claims.Auditing;
using Claims.Contracts;
using Claims.Domain;
using Claims.Persistence;
using Claims.Validation;

namespace Claims.Services;

public sealed class ClaimsService(
    IRepository<Claim> claims,
    IValidator<CreateClaimRequest> validator,
    IAuditer auditer,
    IUnitOfWork unitOfWork) : IClaimsService
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

        claims.Add(claim);
        auditer.AuditClaim(claim.Id, HttpMethods.Post);
        await unitOfWork.SaveChangesAsync();
        return claim;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!await claims.RemoveAsync(id))
        {
            return false;
        }

        auditer.AuditClaim(id, HttpMethods.Delete);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
