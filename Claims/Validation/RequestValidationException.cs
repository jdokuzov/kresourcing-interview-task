using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Validation;

/// <summary>Thrown when a request breaks a business rule.</summary>
public sealed class RequestValidationException(IReadOnlyList<ValidationFailure> failures)
    : Exception("The request breaks one or more business rules.")
{
    public IReadOnlyList<ValidationFailure> Failures { get; } = failures;
}

/// <summary>
/// Turns <see cref="RequestValidationException"/> into a 400 response, in the same format
/// ASP.NET Core uses for its own model validation errors.
/// </summary>
public sealed class RequestValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not RequestValidationException validationException)
        {
            return false;
        }

        var errors = validationException.Failures
            .GroupBy(failure => failure.Field)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.Message).ToArray());

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(
            new ValidationProblemDetails(errors) { Status = StatusCodes.Status400BadRequest },
            options: null,
            contentType: "application/problem+json",
            cancellationToken);
        return true;
    }
}
