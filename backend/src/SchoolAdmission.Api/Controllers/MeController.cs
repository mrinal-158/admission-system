using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Application;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.Http;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me")]
public class MeController(IHoldService holds) : ControllerBase
{
    /// <summary>All applications of the student in X-Student-Id, newest first.</summary>
    [HttpGet("holds")]
    [ProducesResponseType<IReadOnlyList<HoldResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<IReadOnlyList<HoldResponse>> MyHolds(
        [FromHeader(Name = Headers.StudentId)] string? studentId,
        CancellationToken ct) =>
        holds.ListForStudentAsync(StudentId.Parse(studentId), ct);
}
