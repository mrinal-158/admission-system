using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Tests.Integration;

/// <summary>End-to-end behaviour of the public API contract against a real database.</summary>
public class AdmissionApiTests : ApiTestBase
{
    [Fact]
    public async Task Create_and_read_class_follows_the_contract()
    {
        var response = await Admin.PostAsJsonAsync("/classes", new { name = "  Class 6 - Morning Shift ", totalSeats = 120 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Class 6 - Morning Shift", created.RootElement.GetProperty("name").GetString());
        Assert.Equal(120, created.RootElement.GetProperty("totalSeats").GetInt32());
        var id = created.RootElement.GetProperty("id").GetString();

        using var list = JsonDocument.Parse(await Admin.GetStringAsync("/classes"));
        var item = Assert.Single(list.RootElement.EnumerateArray());
        foreach (var field in new[] { "id", "name", "totalSeats", "heldSeats", "confirmedSeats", "availableSeats" })
        {
            Assert.True(item.TryGetProperty(field, out _), $"missing field '{field}'");
        }
        Assert.Equal(120, item.GetProperty("availableSeats").GetInt32());

        var single = await Admin.GetAsync($"/classes/{id}");
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task Create_class_rejects_seat_count_sent_as_text()
    {
        var response = await Admin.PostAsync("/classes", new StringContent(
            """{ "name": "Class 7", "totalSeats": "10" }""", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", await ErrorCodeAsync(response));
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("Class 7", 0)]
    [InlineData("Class 7", -5)]
    public async Task Create_class_rejects_invalid_input(string name, int seats)
    {
        var response = await Admin.PostAsJsonAsync("/classes", new { name, totalSeats = seats });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", await ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Same_class_name_can_be_created_again()
    {
        // The brief defines only 201 for POST /classes, so a repeated name must not be rejected.
        var first = await CreateClassAsync(Admin, 10, "Class 8 - Day Shift");
        var second = await CreateClassAsync(Admin, 5, "Class 8 - Day Shift");

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Unknown_or_malformed_class_id_is_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.GetAsync($"/classes/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.GetAsync("/classes/c_123")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("12345")]
    public async Task Hold_requires_a_valid_student_id_header(string? studentId)
    {
        var cls = await CreateClassAsync(Admin, 5);
        var client = studentId is null ? Admin : Factory.CreateStudentClient(studentId);

        var response = await client.PostAsync($"/classes/{cls.Id}/holds", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_STUDENT_ID", await ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Hold_reserves_a_seat_and_is_visible_in_my_applications()
    {
        var cls = await CreateClassAsync(Admin, 5, "Class 6 - Morning Shift");
        var student = Factory.CreateStudentClient(Student(1));

        var response = await student.PostAsync($"/classes/{cls.Id}/holds", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var hold = (await response.Content.ReadFromJsonAsync<HoldResponse>())!;
        Assert.Equal(cls.Id, hold.ClassId);
        Assert.Equal(Student(1), hold.StudentId);
        Assert.Equal(HoldStatus.Held, hold.Status);
        Assert.Equal(Factory.Clock.GetUtcNow().UtcDateTime.AddSeconds(600), hold.ExpiresAt);

        var seats = await GetClassAsync(Admin, cls.Id);
        Assert.Equal((1, 0, 4), (seats.HeldSeats, seats.ConfirmedSeats, seats.AvailableSeats));

        var mine = (await student.GetFromJsonAsync<List<HoldResponse>>("/me/holds"))!;
        Assert.Equal("Class 6 - Morning Shift", Assert.Single(mine).ClassName);
        Assert.Equal(hold.ExpiresAt, mine[0].ExpiresAt); // same value before and after a DB round trip
    }

    [Fact]
    public async Task Timestamps_are_identical_on_write_and_on_read()
    {
        Factory.Clock.Advance(TimeSpan.FromTicks(1_234_567)); // sub-microsecond ticks on purpose
        var cls = await CreateClassAsync(Admin, 1);
        var placed = (await (await Factory.CreateStudentClient(Student(1)).PostAsync($"/classes/{cls.Id}/holds", null))
            .Content.ReadFromJsonAsync<HoldResponse>())!;

        var read = (await Admin.GetFromJsonAsync<HoldResponse>($"/holds/{placed.HoldId}"))!;

        Assert.Equal(placed.ExpiresAt, read.ExpiresAt);
        Assert.Equal(placed.CreatedAt, read.CreatedAt);
    }

    [Fact]
    public async Task Full_class_returns_409()
    {
        var cls = await CreateClassAsync(Admin, 1);
        await Factory.CreateStudentClient(Student(1)).PostAsync($"/classes/{cls.Id}/holds", null);

        var response = await Factory.CreateStudentClient(Student(2)).PostAsync($"/classes/{cls.Id}/holds", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NO_SEATS_AVAILABLE", await ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Student_cannot_hold_twice_in_the_same_class()
    {
        var cls = await CreateClassAsync(Admin, 5);
        var student = Factory.CreateStudentClient(Student(1));
        await student.PostAsync($"/classes/{cls.Id}/holds", null);

        var response = await student.PostAsync($"/classes/{cls.Id}/holds", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("ALREADY_APPLIED", await ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Expired_hold_releases_its_seat_without_any_api_call()
    {
        var cls = await CreateClassAsync(Admin, 1);
        var first = Factory.CreateStudentClient(Student(1));
        var hold = (await (await first.PostAsync($"/classes/{cls.Id}/holds", null)).Content.ReadFromJsonAsync<HoldResponse>())!;

        Factory.Clock.Advance(TimeSpan.FromSeconds(600));

        Assert.Equal(1, (await GetClassAsync(Admin, cls.Id)).AvailableSeats);
        Assert.Equal(HoldStatus.Expired, (await Admin.GetFromJsonAsync<HoldResponse>($"/holds/{hold.HoldId}"))!.Status);

        var confirm = await first.PostAsync($"/holds/{hold.HoldId}/confirm", null);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Equal("HOLD_EXPIRED", await ErrorCodeAsync(confirm));

        // The freed seat can be taken, and the original student may apply again.
        var second = await Factory.CreateStudentClient(Student(2)).PostAsync($"/classes/{cls.Id}/holds", null);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Confirm_is_idempotent_and_counts_the_seat_once()
    {
        var cls = await CreateClassAsync(Admin, 3);
        var student = Factory.CreateStudentClient(Student(1));
        var hold = (await (await student.PostAsync($"/classes/{cls.Id}/holds", null)).Content.ReadFromJsonAsync<HoldResponse>())!;

        for (var i = 0; i < 3; i++)
        {
            var response = await student.PostAsync($"/holds/{hold.HoldId}/confirm", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HoldStatus.Confirmed, (await response.Content.ReadFromJsonAsync<HoldResponse>())!.Status);
        }

        // Confirmed seats are permanent: time passing does not release them.
        Factory.Clock.Advance(TimeSpan.FromDays(1));
        var seats = await GetClassAsync(Admin, cls.Id);
        Assert.Equal((0, 1, 2), (seats.HeldSeats, seats.ConfirmedSeats, seats.AvailableSeats));
    }

    [Fact]
    public async Task Another_student_cannot_confirm_or_cancel_my_hold()
    {
        var cls = await CreateClassAsync(Admin, 3);
        var hold = (await (await Factory.CreateStudentClient(Student(1)).PostAsync($"/classes/{cls.Id}/holds", null))
            .Content.ReadFromJsonAsync<HoldResponse>())!;
        var intruder = Factory.CreateStudentClient(Student(2));

        var confirm = await intruder.PostAsync($"/holds/{hold.HoldId}/confirm", null);
        var cancel = await intruder.DeleteAsync($"/holds/{hold.HoldId}");

        Assert.Equal(HttpStatusCode.Forbidden, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
        Assert.Equal("HOLD_NOT_OWNED", await ErrorCodeAsync(confirm));
    }

    [Fact]
    public async Task Cancel_releases_the_seat_and_blocks_confirmation()
    {
        var cls = await CreateClassAsync(Admin, 1);
        var student = Factory.CreateStudentClient(Student(1));
        var hold = (await (await student.PostAsync($"/classes/{cls.Id}/holds", null)).Content.ReadFromJsonAsync<HoldResponse>())!;

        Assert.Equal(HttpStatusCode.NoContent, (await student.DeleteAsync($"/holds/{hold.HoldId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await student.DeleteAsync($"/holds/{hold.HoldId}")).StatusCode);

        Assert.Equal(1, (await GetClassAsync(Admin, cls.Id)).AvailableSeats);
        Assert.Equal(HoldStatus.Cancelled, (await Admin.GetFromJsonAsync<HoldResponse>($"/holds/{hold.HoldId}"))!.Status);
        var confirm = await student.PostAsync($"/holds/{hold.HoldId}/confirm", null);
        Assert.Equal("HOLD_CANCELLED", await ErrorCodeAsync(confirm));
    }
}
