using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Tests.Integration;

/// <summary>
/// Fires many requests at the same instant and checks the seat invariant
/// (held + confirmed &lt;= totalSeats) both through the API and directly in the database.
/// </summary>
public class ConcurrencyTests : ApiTestBase
{
    private const int Seats = 10;
    private const int Applicants = 80;

    [Fact]
    public async Task Many_students_racing_for_few_seats_never_overbook()
    {
        var cls = await CreateClassAsync(Admin, Seats);
        var clients = Enumerable.Range(1, Applicants).Select(i => Factory.CreateStudentClient(Student(i))).ToList();

        var statuses = await RaceAsync(clients.Select(c => (Func<Task<HttpResponseMessage>>)(() =>
            c.PostAsync($"/classes/{cls.Id}/holds", null))));

        Assert.Equal(Seats, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(Applicants - Seats, statuses.Count(s => s == HttpStatusCode.Conflict));
        await AssertSeatInvariantAsync(Factory, cls.Id, expectedTaken: Seats);
    }

    [Fact]
    public async Task Same_student_double_clicking_gets_exactly_one_hold()
    {
        var cls = await CreateClassAsync(Admin, Seats);
        var student = Factory.CreateStudentClient(Student(1));

        var statuses = await RaceAsync(Enumerable.Range(0, 20).Select(_ => (Func<Task<HttpResponseMessage>>)(() =>
            student.PostAsync($"/classes/{cls.Id}/holds", null))));

        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        await AssertSeatInvariantAsync(Factory, cls.Id, expectedTaken: 1);
    }

    [Fact]
    public async Task Concurrent_confirms_count_the_seat_once()
    {
        var cls = await CreateClassAsync(Admin, Seats);
        var student = Factory.CreateStudentClient(Student(1));
        var hold = (await (await student.PostAsync($"/classes/{cls.Id}/holds", null)).Content.ReadFromJsonAsync<HoldResponse>())!;

        var statuses = await RaceAsync(Enumerable.Range(0, 20).Select(_ => (Func<Task<HttpResponseMessage>>)(() =>
            student.PostAsync($"/holds/{hold.HoldId}/confirm", null))));

        Assert.All(statuses, s => Assert.Equal(HttpStatusCode.OK, s));
        Assert.Equal(1, (await GetClassAsync(Admin, cls.Id)).ConfirmedSeats);
    }

    /// <summary>
    /// Expiry boundary: the last seat's holder pays at the instant their hold expires while
    /// another student applies for that seat. The clock is frozen, so this checks that both
    /// paths agree on the boundary (before expiresAt the payment wins, at expiresAt the hold
    /// has lapsed and the newcomer wins), not the lock; the other tests here cover the lock.
    /// </summary>
    [Theory]
    [InlineData(-1, HttpStatusCode.OK, HttpStatusCode.Conflict)]
    [InlineData(0, HttpStatusCode.Conflict, HttpStatusCode.Created)]
    public async Task Payment_racing_a_new_applicant_at_expiry_gives_the_seat_to_exactly_one(
        int secondsFromExpiry, HttpStatusCode expectedConfirm, HttpStatusCode expectedApply)
    {
        var cls = await CreateClassAsync(Admin, seats: 1);
        var holder = Factory.CreateStudentClient(Student(1));
        var newcomer = Factory.CreateStudentClient(Student(2));
        var hold = (await (await holder.PostAsync($"/classes/{cls.Id}/holds", null)).Content.ReadFromJsonAsync<HoldResponse>())!;

        Factory.Clock.Advance(TimeSpan.FromSeconds(600 + secondsFromExpiry));
        var statuses = await RaceAsync(
        [
            () => holder.PostAsync($"/holds/{hold.HoldId}/confirm", null),
            () => newcomer.PostAsync($"/classes/{cls.Id}/holds", null),
        ]);

        Assert.Equal(expectedConfirm, statuses[0]);
        Assert.Equal(expectedApply, statuses[1]);
        await AssertSeatInvariantAsync(Factory, cls.Id, expectedTaken: 1);
    }

    /// <summary>
    /// Pay and cancel clicked together (two tabs, two devices). Whichever runs first decides;
    /// the other side must then fail consistently, never a mix of "paid" and "cancelled".
    /// </summary>
    [Fact]
    public async Task Cancel_racing_confirm_ends_in_one_consistent_outcome()
    {
        var cls = await CreateClassAsync(Admin, Seats);
        var student = Factory.CreateStudentClient(Student(1));
        var hold = (await (await student.PostAsync($"/classes/{cls.Id}/holds", null)).Content.ReadFromJsonAsync<HoldResponse>())!;

        var requests = Enumerable.Range(0, 20).Select(i => (Func<Task<HttpResponseMessage>>)(i % 2 == 0
            ? () => student.PostAsync($"/holds/{hold.HoldId}/confirm", null)
            : () => student.DeleteAsync($"/holds/{hold.HoldId}")));
        var statuses = await RaceAsync(requests);
        var confirms = statuses.Where((_, i) => i % 2 == 0).ToList();
        var cancels = statuses.Where((_, i) => i % 2 == 1).ToList();

        var final = (await Admin.GetFromJsonAsync<HoldResponse>($"/holds/{hold.HoldId}"))!.Status;
        var seats = await GetClassAsync(Admin, cls.Id);
        if (final == HoldStatus.Confirmed)
        {
            Assert.All(confirms, s => Assert.Equal(HttpStatusCode.OK, s));
            Assert.All(cancels, s => Assert.Equal(HttpStatusCode.Conflict, s));
            Assert.Equal((0, 1), (seats.HeldSeats, seats.ConfirmedSeats));
        }
        else
        {
            Assert.Equal(HoldStatus.Cancelled, final);
            Assert.All(confirms, s => Assert.Equal(HttpStatusCode.Conflict, s));
            Assert.All(cancels, s => Assert.Equal(HttpStatusCode.NoContent, s));
            Assert.Equal((0, 0), (seats.HeldSeats, seats.ConfirmedSeats));
        }
    }

    /// <summary>
    /// Bonus requirement: two independent API hosts (separate DI containers and connection pools,
    /// as two processes would have) share one database. In-process locks would not help here;
    /// the PostgreSQL row lock does.
    /// </summary>
    [Fact]
    public async Task Two_api_instances_on_one_database_never_overbook()
    {
        await using var second = new ApiFactory(databaseName: Factory.DatabaseName);
        var cls = await CreateClassAsync(Admin, Seats);

        var requests = Enumerable.Range(1, Applicants).Select(i =>
        {
            var host = i % 2 == 0 ? Factory : second;
            var client = host.CreateStudentClient(Student(i));
            return (Func<Task<HttpResponseMessage>>)(() => client.PostAsync($"/classes/{cls.Id}/holds", null));
        });
        var statuses = await RaceAsync(requests);

        Assert.Equal(Seats, statuses.Count(s => s == HttpStatusCode.Created));
        await AssertSeatInvariantAsync(Factory, cls.Id, expectedTaken: Seats);
    }

    /// <summary>Starts all requests behind a barrier so they hit the server together.</summary>
    private static async Task<HttpStatusCode[]> RaceAsync(IEnumerable<Func<Task<HttpResponseMessage>>> requests)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = requests.Select(async send =>
        {
            await gate.Task;
            using var response = await send();
            return response.StatusCode;
        }).ToList();

        gate.SetResult();
        return await Task.WhenAll(tasks);
    }

    private static async Task AssertSeatInvariantAsync(ApiFactory factory, Guid classId, int expectedTaken)
    {
        var taken = await factory.WithDbAsync(db => db.Holds.CountAsync(h =>
            h.ClassId == classId && (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed)));
        Assert.Equal(expectedTaken, taken);
    }
}
