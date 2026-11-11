using System.Text.Json.Serialization;

namespace SchoolAdmission.Api.Domain;

/// <summary>
/// Lifecycle of a seat hold (an admission application).
/// HELD -> CONFIRMED (fee paid), HELD -> CANCELLED (student withdrew),
/// HELD -> EXPIRED (fee not paid before <see cref="Hold.ExpiresAt"/>).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<HoldStatus>))]
public enum HoldStatus
{
    [JsonStringEnumMemberName("HELD")] Held,
    [JsonStringEnumMemberName("CONFIRMED")] Confirmed,
    [JsonStringEnumMemberName("EXPIRED")] Expired,
    [JsonStringEnumMemberName("CANCELLED")] Cancelled,
}
