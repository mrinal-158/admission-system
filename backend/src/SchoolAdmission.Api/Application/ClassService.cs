using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.Infrastructure;

namespace SchoolAdmission.Api.Application;

public interface IClassService
{
    Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken ct);
    Task<IReadOnlyList<ClassResponse>> ListAsync(CancellationToken ct);
    Task<ClassResponse> GetAsync(Guid classId, CancellationToken ct);
}

public class ClassService(AdmissionDbContext db, TimeProvider time) : IClassService
{
    public async Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken ct)
    {
        var schoolClass = SchoolClass.Create(request.Name, request.TotalSeats, time.UtcNowForDb());
        db.Classes.Add(schoolClass);
        await db.SaveChangesAsync(ct);

        return new ClassResponse(schoolClass.Id, schoolClass.Name, schoolClass.TotalSeats, 0, 0, schoolClass.TotalSeats);
    }

    public async Task<IReadOnlyList<ClassResponse>> ListAsync(CancellationToken ct) =>
        await SeatSummaries(db.Classes.OrderBy(c => c.CreatedAt)).ToListAsync(ct);

    public async Task<ClassResponse> GetAsync(Guid classId, CancellationToken ct) =>
        await SeatSummaries(db.Classes.Where(c => c.Id == classId)).SingleOrDefaultAsync(ct)
        ?? throw Errors.ClassNotFound(classId.ToString());

    /// <summary>
    /// Seat accounting in one SQL round trip. A HELD row counts only until it expires,
    /// which is what makes expired seats free up without any background job.
    /// </summary>
    private IQueryable<ClassResponse> SeatSummaries(IQueryable<SchoolClass> classes)
    {
        var now = time.UtcNowForDb();
        return classes
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                Held = c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                Confirmed = c.Holds.Count(h => h.Status == HoldStatus.Confirmed),
            })
            .Select(c => new ClassResponse(
                c.Id, c.Name, c.TotalSeats, c.Held, c.Confirmed, c.TotalSeats - c.Held - c.Confirmed));
    }
}
