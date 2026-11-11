using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Tests.Unit;

public class HoldTests
{
    private static readonly DateTime T0 = new(2026, 11, 1, 4, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Duration = TimeSpan.FromMinutes(10);
    private static readonly StudentId Owner = StudentId.Parse("20152691234567890");
    private static readonly StudentId Other = StudentId.Parse("20152691234567891");

    private static Hold NewHold() => Hold.Place(Guid.NewGuid(), Owner, T0, Duration);

    [Fact]
    public void New_hold_is_held_until_expiry_then_effectively_expired()
    {
        var hold = NewHold();

        Assert.Equal(T0 + Duration, hold.ExpiresAt);
        Assert.Equal(HoldStatus.Held, hold.EffectiveStatus(T0 + Duration - TimeSpan.FromSeconds(1)));
        Assert.Equal(HoldStatus.Expired, hold.EffectiveStatus(T0 + Duration));
    }

    [Fact]
    public void Confirm_within_window_confirms_and_is_idempotent()
    {
        var hold = NewHold();

        hold.Confirm(Owner, T0.AddMinutes(1));
        var confirmedAt = hold.ConfirmedAt;
        hold.Confirm(Owner, T0.AddMinutes(2));

        Assert.Equal(HoldStatus.Confirmed, hold.Status);
        Assert.Equal(confirmedAt, hold.ConfirmedAt);
        // A confirmed seat never expires.
        Assert.Equal(HoldStatus.Confirmed, hold.EffectiveStatus(T0.AddDays(1)));
    }

    [Fact]
    public void Confirm_after_expiry_is_rejected()
    {
        var hold = NewHold();

        var ex = Assert.Throws<DomainException>(() => hold.Confirm(Owner, T0 + Duration));

        Assert.Equal("HOLD_EXPIRED", ex.Code);
        Assert.Equal(HoldStatus.Held, hold.Status); // nothing mutated
    }

    [Fact]
    public void Only_the_owner_can_confirm_or_cancel()
    {
        var hold = NewHold();

        Assert.Equal("HOLD_NOT_OWNED", Assert.Throws<DomainException>(() => hold.Confirm(Other, T0)).Code);
        Assert.Equal("HOLD_NOT_OWNED", Assert.Throws<DomainException>(() => hold.Cancel(Other, T0)).Code);
    }

    [Fact]
    public void Cancelled_hold_cannot_be_confirmed()
    {
        var hold = NewHold();
        hold.Cancel(Owner, T0.AddMinutes(1));

        Assert.Equal("HOLD_CANCELLED", Assert.Throws<DomainException>(() => hold.Confirm(Owner, T0.AddMinutes(2))).Code);
    }

    [Fact]
    public void Confirmed_hold_cannot_be_cancelled()
    {
        var hold = NewHold();
        hold.Confirm(Owner, T0.AddMinutes(1));

        Assert.Equal("HOLD_ALREADY_CONFIRMED", Assert.Throws<DomainException>(() => hold.Cancel(Owner, T0.AddMinutes(2))).Code);
    }

    [Fact]
    public void Cancel_of_inactive_hold_is_a_no_op()
    {
        var expired = NewHold();
        expired.Cancel(Owner, T0 + Duration);
        Assert.Equal(HoldStatus.Expired, expired.EffectiveStatus(T0 + Duration));

        var cancelled = NewHold();
        cancelled.Cancel(Owner, T0);
        cancelled.Cancel(Owner, T0.AddMinutes(1));
        Assert.Equal(HoldStatus.Cancelled, cancelled.Status);
        Assert.Equal(T0, cancelled.CancelledAt);
    }
}
