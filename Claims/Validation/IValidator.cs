namespace Claims.Validation;

/// <summary>A broken business rule.</summary>
public sealed record ValidationFailure(string Field, string Message);

/// <summary>Checks the business rules for a request.</summary>
public interface IValidator<in TRequest>
{
    /// <returns>All broken rules; empty if the request is valid.</returns>
    Task<IReadOnlyList<ValidationFailure>> ValidateAsync(TRequest request);
}

public static class ValidatorExtensions
{
    /// <exception cref="RequestValidationException">The request breaks a business rule.</exception>
    public static async Task ThrowIfInvalidAsync<TRequest>(this IValidator<TRequest> validator, TRequest request)
    {
        var failures = await validator.ValidateAsync(request);
        if (failures.Count > 0)
        {
            throw new RequestValidationException(failures);
        }
    }
}
