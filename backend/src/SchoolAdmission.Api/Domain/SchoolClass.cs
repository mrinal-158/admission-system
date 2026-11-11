namespace SchoolAdmission.Api.Domain;

/// <summary>
/// One admission unit with its own seat pool. Each shift is its own class,
/// e.g. "Class 6 - Morning Shift" and "Class 6 - Day Shift".
/// </summary>
public class SchoolClass
{
    public const int MaxNameLength = 100;
    public const int MaxSeats = 10_000;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;

    public int TotalSeats { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<Hold> _holds = [];

    /// <summary>Read-only: holds are created through the hold workflow, never added here.</summary>
    public IReadOnlyCollection<Hold> Holds => _holds;

    private SchoolClass() { } // EF Core

    public static SchoolClass Create(string name, int totalSeats, DateTime now)
    {
        return new SchoolClass
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            TotalSeats = totalSeats,
            CreatedAt = now,
        };
    }
}
