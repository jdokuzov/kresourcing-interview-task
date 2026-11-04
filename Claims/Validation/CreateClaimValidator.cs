using Claims.Contracts;
using Claims.Domain;
using Claims.Persistence;

namespace Claims.Validation;

public sealed class CreateClaimValidator(IRepository<Cover> covers) : IValidator<CreateClaimRequest>
{
    private const decimal MaxDamageCost = 100_000m;

    public async Task<IReadOnlyList<ValidationFailure>> ValidateAsync(CreateClaimRequest request)
    {
        var failures = new List<ValidationFailure>();

        if (request.DamageCost < 0)
        {
            failures.Add(new(nameof(request.DamageCost), "DamageCost cannot be negative."));
        }
        else if (request.DamageCost > MaxDamageCost)
        {
            failures.Add(new(nameof(request.DamageCost), $"DamageCost cannot exceed {MaxDamageCost}."));
        }

        var cover = await covers.GetByIdAsync(request.CoverId);
        if (cover is null)
        {
            failures.Add(new(nameof(request.CoverId), $"Cover '{request.CoverId}' does not exist."));
        }
        else if (request.Created.Date < cover.StartDate.Date || request.Created.Date > cover.EndDate.Date)
        {
            failures.Add(new(nameof(request.Created), "Created must be within the period of the related cover."));
        }

        return failures;
    }
}
