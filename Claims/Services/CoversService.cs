using Claims.Auditing;
using Claims.Contracts;
using Claims.Domain;
using Claims.Persistence;
using Claims.Validation;

namespace Claims.Services;

public sealed class CoversService(
    IRepository<Cover> covers,
    IValidator<CreateCoverRequest> validator,
    IPremiumCalculator premiumCalculator,
    IAuditer auditer,
    IUnitOfWork unitOfWork) : ICoversService
{
    public Task<IReadOnlyList<Cover>> GetAllAsync() => covers.GetAllAsync();

    public Task<Cover?> GetByIdAsync(string id) => covers.GetByIdAsync(id);

    public async Task<Cover> CreateAsync(CreateCoverRequest request)
    {
        await validator.ThrowIfInvalidAsync(request);

        var cover = new Cover
        {
            Id = Guid.NewGuid().ToString(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Type = request.Type,
            Premium = premiumCalculator.Compute(request.StartDate, request.EndDate, request.Type)
        };

        covers.Add(cover);
        auditer.AuditCover(cover.Id, HttpMethods.Post);
        await unitOfWork.SaveChangesAsync();
        return cover;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!await covers.RemoveAsync(id))
        {
            return false;
        }

        auditer.AuditCover(id, HttpMethods.Delete);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType) =>
        premiumCalculator.Compute(startDate, endDate, coverType);
}
