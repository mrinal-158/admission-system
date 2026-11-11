using System.ComponentModel.DataAnnotations;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Contracts;

// Field names below are the public API contract; do not rename or remove them.

public record CreateClassRequest(
    [Required(ErrorMessage = "Class name is required.")]
    [StringLength(SchoolClass.MaxNameLength, ErrorMessage = "Class name must be at most {1} characters.")]
    string Name,
    [Range(1, SchoolClass.MaxSeats, ErrorMessage = "Total seats must be between {1} and {2}.")]
    int TotalSeats);

public record ClassResponse(
    Guid Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);

public record HoldResponse(
    Guid HoldId,
    Guid ClassId,
    string ClassName,
    string StudentId,
    HoldStatus Status,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime? ConfirmedAt);
