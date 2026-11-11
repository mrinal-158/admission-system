using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Api.Application;

public class HoldOptions
{
    /// <summary>Configuration key; set through the HOLD_DURATION_SECONDS environment variable.</summary>
    public const string DurationKey = "HOLD_DURATION_SECONDS";

    [Range(1, 7 * 24 * 60 * 60)]
    public int DurationSeconds { get; set; } = 600;

    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
}
