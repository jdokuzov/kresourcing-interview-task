using Claims.Auditing;
using Claims.Contracts;
using Claims.Domain;
using Claims.Persistence;

namespace Claims.Services;

public sealed class CoversService(
    IRepository<Cover> covers,
    IPremiumCalculator premiumCalculator,
    IAuditer auditer) : ICoversService
{
    public Task<IReadOnlyList<Cover>> GetAllAsync() => covers.GetAllAsync();

    public Task<Cover?> GetByIdAsync(string id) => covers.GetByIdAsync(id);

    public async Task<Cover> CreateAsync(CreateCoverRequest request)
    {
        var cover = new Cover
        {
            Id = Guid.NewGuid().ToString(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Type = request.Type,
            Premium = premiumCalculator.Compute(request.StartDate, request.EndDate, request.Type)
        };

        await covers.AddAsync(cover);
        await auditer.AuditCoverAsync(cover.Id, HttpMethods.Post);
        return cover;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var deleted = await covers.DeleteAsync(id);
        if (deleted)
        {
            await auditer.AuditCoverAsync(id, HttpMethods.Delete);
        }

        return deleted;
    }

    public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType) =>
        premiumCalculator.Compute(startDate, endDate, coverType);
}
