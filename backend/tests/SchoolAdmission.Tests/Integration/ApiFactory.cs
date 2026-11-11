using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using SchoolAdmission.Api.Infrastructure;

namespace SchoolAdmission.Tests.Integration;

/// <summary>
/// Hosts the real API in memory against a real PostgreSQL database (locking behaviour cannot be
/// faked), with a controllable clock so expiry is tested without sleeping.
/// </summary>
/// <remarks>
/// Each factory owns a throw-away database unless <paramref name="databaseName"/> is shared,
/// which is how two "API processes" on one database are simulated.
/// Server: env <c>TEST_DB_CONNECTION</c>, defaulting to a local postgres/postgres.
/// </remarks>
public sealed class ApiFactory(string? databaseName = null, int holdDurationSeconds = 600)
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string ServerConnection =
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    public string DatabaseName { get; } = databaseName ?? $"sa_test_{Guid.NewGuid():N}";

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 1, 4, 0, 0, TimeSpan.Zero));

    private string ConnectionString =>
        new NpgsqlConnectionStringBuilder(ServerConnection) { Database = DatabaseName }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("HOLD_DURATION_SECONDS", holdDurationSeconds.ToString());
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }

    /// <summary>Starts the host, which runs migrations and so creates the database.</summary>
    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }

    public HttpClient CreateStudentClient(string studentId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Student-Id", studentId);
        return client;
    }

    public async Task<T> WithDbAsync<T>(Func<AdmissionDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AdmissionDbContext>());
    }

    public async Task DropDatabaseAsync() =>
        await WithDbAsync(db => db.Database.EnsureDeletedAsync());

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (databaseName is null)
        {
            await DropDatabaseAsync();
        }
        await DisposeAsync();
    }
}
