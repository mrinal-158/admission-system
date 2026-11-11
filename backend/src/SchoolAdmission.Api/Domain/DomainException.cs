namespace SchoolAdmission.Api.Domain;

public enum ErrorKind
{
    Validation,
    NotFound,
    Forbidden,
    Conflict,
}

/// <summary>
/// An expected business-rule failure. <see cref="Code"/> is a stable, machine-readable
/// identifier the UI maps to a user-facing message; HTTP mapping lives in the Http layer.
/// </summary>
public sealed class DomainException(ErrorKind kind, string code, string message) : Exception(message)
{
    public ErrorKind Kind { get; } = kind;
    public string Code { get; } = code;

    public static DomainException Validation(string code, string message) => new(ErrorKind.Validation, code, message);
    public static DomainException NotFound(string code, string message) => new(ErrorKind.NotFound, code, message);
    public static DomainException Forbidden(string code, string message) => new(ErrorKind.Forbidden, code, message);
    public static DomainException Conflict(string code, string message) => new(ErrorKind.Conflict, code, message);
}

/// <summary>Catalogue of every business error the API can return.</summary>
public static class Errors
{
    public static DomainException ClassNotFound(string classId) =>
        DomainException.NotFound("CLASS_NOT_FOUND", $"Class '{classId}' was not found.");

    public static DomainException HoldNotFound(string holdId) =>
        DomainException.NotFound("HOLD_NOT_FOUND", $"Application '{holdId}' was not found.");

    public static DomainException NoSeatsAvailable() =>
        DomainException.Conflict("NO_SEATS_AVAILABLE", "No seats are available in this class.");

    public static DomainException AlreadyApplied() =>
        DomainException.Conflict("ALREADY_APPLIED", "This student already has an active or confirmed application for this class.");

    public static DomainException HoldNotOwned() =>
        DomainException.Forbidden("HOLD_NOT_OWNED", "This application belongs to a different student.");

    public static DomainException HoldExpired() =>
        DomainException.Conflict("HOLD_EXPIRED", "The hold period has ended; the seat was released.");

    public static DomainException HoldCancelled() =>
        DomainException.Conflict("HOLD_CANCELLED", "This application was cancelled.");

    public static DomainException HoldAlreadyConfirmed() =>
        DomainException.Conflict("HOLD_ALREADY_CONFIRMED", "Admission is already confirmed and cannot be cancelled here.");
}
