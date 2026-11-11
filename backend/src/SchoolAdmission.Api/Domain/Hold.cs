namespace SchoolAdmission.Api.Domain;

/// <summary>
/// A student's application for a seat. The seat is reserved while the hold is HELD and
/// not past <see cref="ExpiresAt"/>, and permanently once CONFIRMED.
/// </summary>
/// <remarks>
/// Expiry is time-based, not event-based: a HELD row whose <see cref="ExpiresAt"/> has passed
/// is EXPIRED even if the stored status still says HELD. Every read goes through
/// <see cref="EffectiveStatus"/>, so a seat frees itself the moment the hold expires
/// without any job or API call having to run.
/// </remarks>
public class Hold
{
    public Guid Id { get; private set; }
    public Guid ClassId { get; private set; }
    public SchoolClass Class { get; private set; } = null!;
    public string StudentId { get; private set; } = null!;
    public HoldStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private Hold() { } // EF Core

    public static Hold Place(Guid classId, StudentId studentId, DateTime now, TimeSpan duration) => new()
    {
        Id = Guid.CreateVersion7(),
        ClassId = classId,
        StudentId = studentId.Value,
        Status = HoldStatus.Held,
        CreatedAt = now,
        ExpiresAt = now + duration,
    };

    public HoldStatus EffectiveStatus(DateTime now) =>
        Status == HoldStatus.Held && now >= ExpiresAt ? HoldStatus.Expired : Status;

    /// <summary>Pays the admission fee. Idempotent: confirming twice is a no-op.</summary>
    public void Confirm(StudentId studentId, DateTime now)
    {
        EnsureOwnedBy(studentId);

        switch (EffectiveStatus(now))
        {
            case HoldStatus.Confirmed:
                return;
            case HoldStatus.Cancelled:
                throw Errors.HoldCancelled();
            case HoldStatus.Expired:
                throw Errors.HoldExpired();
        }

        Status = HoldStatus.Confirmed;
        ConfirmedAt = now;
    }

    /// <summary>Withdraws the application. Idempotent for holds that are already inactive.</summary>
    public void Cancel(StudentId studentId, DateTime now)
    {
        EnsureOwnedBy(studentId);

        switch (EffectiveStatus(now))
        {
            case HoldStatus.Confirmed:
                throw Errors.HoldAlreadyConfirmed();
            case HoldStatus.Cancelled or HoldStatus.Expired:
                return;
        }

        Status = HoldStatus.Cancelled;
        CancelledAt = now;
    }

    private void EnsureOwnedBy(StudentId studentId)
    {
        if (StudentId != studentId.Value)
        {
            throw Errors.HoldNotOwned();
        }
    }
}
