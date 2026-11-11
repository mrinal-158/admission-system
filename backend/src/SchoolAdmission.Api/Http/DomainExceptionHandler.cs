using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Http;

/// <summary>
/// Turns <see cref="DomainException"/> into an RFC 7807 problem response carrying a stable
/// <c>code</c> the UI can map to a friendly message. Anything else falls through to the
/// default handler, which returns a generic 500 problem without leaking internals.
/// </summary>
public class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not DomainException domain)
        {
            return false;
        }

        var status = domain.Kind switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = domain.Message,
                Extensions = { ["code"] = domain.Code },
            },
        });
    }
}
