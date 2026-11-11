using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Application;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.Http;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public class HoldsController(IHoldService holds) : ControllerBase
{
    [HttpGet("{holdId:guid}")]
    [ProducesResponseType<HoldResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<HoldResponse> Get(Guid holdId, CancellationToken ct) => holds.GetAsync(holdId, ct);

    /// <summary>Pays the admission fee (simulated). Idempotent.</summary>
    [HttpPost("{holdId:guid}/confirm")]
    [ProducesResponseType<HoldResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<HoldResponse> Confirm(
        Guid holdId,
        [FromHeader(Name = Headers.StudentId)] string? studentId,
        CancellationToken ct) =>
        holds.ConfirmAsync(holdId, StudentId.Parse(studentId), ct);

    [HttpDelete("{holdId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid holdId,
        [FromHeader(Name = Headers.StudentId)] string? studentId,
        CancellationToken ct)
    {
        await holds.CancelAsync(holdId, StudentId.Parse(studentId), ct);
        return NoContent();
    }
}
