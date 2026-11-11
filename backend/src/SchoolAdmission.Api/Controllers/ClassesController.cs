using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Application;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.Http;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController(IClassService classes, IHoldService holds) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ClassResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassResponse>> Create(CreateClassRequest request, CancellationToken ct)
    {
        var created = await classes.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { classId = created.Id }, created);
    }

    [HttpGet]
    public Task<IReadOnlyList<ClassResponse>> List(CancellationToken ct) => classes.ListAsync(ct);

    [HttpGet("{classId:guid}")]
    [ProducesResponseType<ClassResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ClassResponse> Get(Guid classId, CancellationToken ct) => classes.GetAsync(classId, ct);

    /// <summary>Applies for a seat: holds it for HOLD_DURATION_SECONDS until the fee is paid.</summary>
    [HttpPost("{classId:guid}/holds")]
    [ProducesResponseType<HoldResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HoldResponse>> PlaceHold(
        Guid classId,
        [FromHeader(Name = Headers.StudentId)] string? studentId,
        CancellationToken ct)
    {
        var hold = await holds.PlaceAsync(classId, StudentId.Parse(studentId), ct);
        return CreatedAtAction(nameof(HoldsController.Get), "Holds", new { holdId = hold.HoldId }, hold);
    }
}
