using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.Infrastructure;

namespace SchoolAdmission.Api.Application;

public interface IHoldService
{
    Task<HoldResponse> PlaceAsync(Guid classId, StudentId studentId, CancellationToken ct);
    Task<HoldResponse> GetAsync(Guid holdId, CancellationToken ct);
    Task<HoldResponse> ConfirmAsync(Guid holdId, StudentId studentId, CancellationToken ct);
    Task CancelAsync(Guid holdId, StudentId studentId, CancellationToken ct);
    Task<IReadOnlyList<HoldResponse>> ListForStudentAsync(StudentId studentId, CancellationToken ct);
}

/// <summary>
/// Seat holds. Invariant: for every class, HELD (unexpired) + CONFIRMED &lt;= TotalSeats,
/// no matter how many requests, threads or API processes run at once.
/// </summary>
/// <remarks>
/// Concurrency strategy: every write that can change a class's seat usage first takes a
/// row lock on that class (<c>SELECT ... FOR UPDATE</c>) inside a transaction. The lock lives
/// in PostgreSQL, not in process memory, so it also serialises separate API instances that
/// share the database. Locks are always taken class first, then hold, so writers cannot deadlock.
/// Reads take no locks.
/// </remarks>
public class HoldService(AdmissionDbContext db, TimeProvider time, IOptions<HoldOptions> options) : IHoldService
{
    private DateTime Now => time.UtcNowForDb();

    public async Task<HoldResponse> PlaceAsync(Guid classId, StudentId studentId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var schoolClass = await LockClassAsync(classId, ct) ?? throw Errors.ClassNotFound(classId.ToString());
        var now = Now;

        // Persist expiry for holds that have timed out, so their rows stop counting as active
        // and stop blocking the partial unique index for that student.
        await db.Holds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), ct);

        var active = db.Holds.Where(h =>
            h.ClassId == classId && (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed));

        if (await active.AnyAsync(h => h.StudentId == studentId.Value, ct))
        {
            throw Errors.AlreadyApplied();
        }

        if (await active.CountAsync(ct) >= schoolClass.TotalSeats)
        {
            throw Errors.NoSeatsAvailable();
        }

        var hold = Hold.Place(classId, studentId, now, options.Value.Duration);
        db.Holds.Add(hold);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation(AdmissionDbContext.ActiveHoldPerStudentIndex))
        {
            throw Errors.AlreadyApplied();
        }

        await tx.CommitAsync(ct);
        return ToResponse(hold, schoolClass.Name, now);
    }

    public async Task<HoldResponse> GetAsync(Guid holdId, CancellationToken ct)
    {
        var hold = await db.Holds.AsNoTracking().Include(h => h.Class).SingleOrDefaultAsync(h => h.Id == holdId, ct)
                   ?? throw Errors.HoldNotFound(holdId.ToString());
        return ToResponse(hold, hold.Class.Name, Now);
    }

    public Task<HoldResponse> ConfirmAsync(Guid holdId, StudentId studentId, CancellationToken ct) =>
        ChangeHoldAsync(holdId, hold => hold.Confirm(studentId, Now), ct);

    public Task CancelAsync(Guid holdId, StudentId studentId, CancellationToken ct) =>
        ChangeHoldAsync(holdId, hold => hold.Cancel(studentId, Now), ct);

    public async Task<IReadOnlyList<HoldResponse>> ListForStudentAsync(StudentId studentId, CancellationToken ct)
    {
        var holds = await db.Holds.AsNoTracking()
            .Include(h => h.Class)
            .Where(h => h.StudentId == studentId.Value)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);

        var now = Now;
        return holds.Select(h => ToResponse(h, h.Class.Name, now)).ToList();
    }

    /// <summary>Applies a state transition to a hold while holding its class's lock.</summary>
    private async Task<HoldResponse> ChangeHoldAsync(Guid holdId, Action<Hold> transition, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var classId = await db.Holds.Where(h => h.Id == holdId).Select(h => (Guid?)h.ClassId).SingleOrDefaultAsync(ct)
                      ?? throw Errors.HoldNotFound(holdId.ToString());
        var schoolClass = await LockClassAsync(classId, ct) ?? throw Errors.ClassNotFound(classId.ToString());

        // Loaded after the lock, so we see the latest committed state.
        var hold = await db.Holds.SingleAsync(h => h.Id == holdId, ct);
        transition(hold);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToResponse(hold, schoolClass.Name, Now);
    }

    private async Task<SchoolClass?> LockClassAsync(Guid classId, CancellationToken ct)
    {
        // Materialised without composing further LINQ so EF sends this SQL verbatim.
        var rows = await db.Classes
            .FromSql($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
            .ToListAsync(ct);
        return rows.SingleOrDefault();
    }

    private static HoldResponse ToResponse(Hold hold, string className, DateTime now) => new(
        hold.Id,
        hold.ClassId,
        className,
        hold.StudentId,
        hold.EffectiveStatus(now),
        hold.ExpiresAt,
        hold.CreatedAt,
        hold.ConfirmedAt);
}
