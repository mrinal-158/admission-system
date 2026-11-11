namespace SchoolAdmission.Api.Domain;

/// <summary>
/// A student's 17-digit birth registration number, used as their identity.
/// Wrapping it in a value object means an unvalidated string can never reach the services.
/// </summary>
public readonly record struct StudentId
{
    public const int Length = 17;

    public string Value { get; }

    private StudentId(string value) => Value = value;

    public static bool TryParse(string? raw, out StudentId studentId)
    {
        var candidate = raw?.Trim();
        if (candidate is { Length: Length } && candidate.All(char.IsAsciiDigit))
        {
            studentId = new StudentId(candidate);
            return true;
        }

        studentId = default;
        return false;
    }

    public static StudentId Parse(string? raw) =>
        TryParse(raw, out var studentId)
            ? studentId
            : throw DomainException.Validation(
                "INVALID_STUDENT_ID",
                $"X-Student-Id header must be a {Length}-digit birth registration number.");

    public override string ToString() => Value;
}
