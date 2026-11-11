using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Tests.Unit;

public class StudentIdTests
{
    [Theory]
    [InlineData("20152691234567890")]
    [InlineData(" 20152691234567890 ")]
    public void Accepts_17_digits(string raw)
    {
        Assert.True(StudentId.TryParse(raw, out var id));
        Assert.Equal("20152691234567890", id.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2015269123456789")]     // 16 digits
    [InlineData("201526912345678901")]   // 18 digits
    [InlineData("2015269123456789O")]    // letter O
    [InlineData("২০১৫২৬৯১২৩৪৫৬৭৮৯০")]    // Bangla digits are rejected; the registry uses ASCII
    public void Rejects_anything_else(string? raw)
    {
        Assert.False(StudentId.TryParse(raw, out _));
        Assert.Equal("INVALID_STUDENT_ID", Assert.Throws<DomainException>(() => StudentId.Parse(raw)).Code);
    }
}
