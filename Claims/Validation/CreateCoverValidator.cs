using Claims.Contracts;

namespace Claims.Validation;

public sealed class CreateCoverValidator(TimeProvider timeProvider) : IValidator<CreateCoverRequest>
{
    public Task<IReadOnlyList<ValidationFailure>> ValidateAsync(CreateCoverRequest request)
    {
        var failures = new List<ValidationFailure>();

        var today = timeProvider.GetUtcNow().UtcDateTime.Date;
        if (request.StartDate.Date < today)
        {
            failures.Add(new(nameof(request.StartDate), "StartDate cannot be in the past."));
        }

        if (request.EndDate <= request.StartDate)
        {
            failures.Add(new(nameof(request.EndDate), "EndDate must be after StartDate."));
        }
        else if (request.EndDate.AddYears(-1) > request.StartDate)
        {
            failures.Add(new(nameof(request.EndDate), "The insurance period cannot exceed 1 year."));
        }

        return Task.FromResult<IReadOnlyList<ValidationFailure>>(failures);
    }
}
