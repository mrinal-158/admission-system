namespace SchoolAdmission.Api.Application;

public static class TimeProviderExtensions
{
    /// <summary>
    /// UTC now truncated to PostgreSQL's microsecond precision, so a timestamp returned
    /// straight after a write is identical to the same timestamp read back later.
    /// </summary>
    public static DateTime UtcNowForDb(this TimeProvider time)
    {
        var now = time.GetUtcNow().UtcDateTime;
        return now.AddTicks(-(now.Ticks % (TimeSpan.TicksPerMillisecond / 1000)));
    }
}
