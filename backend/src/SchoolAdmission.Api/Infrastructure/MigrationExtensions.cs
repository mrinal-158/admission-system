using Microsoft.EntityFrameworkCore;

namespace SchoolAdmission.Api.Infrastructure;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations on startup, retrying while PostgreSQL is still booting.
    /// EF Core takes a database-level migration lock, so several API instances starting together is safe.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app, int maxAttempts = 10)
    {
        var logger = app.Logger;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<AdmissionDbContext>().Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && ex is Npgsql.NpgsqlException or TimeoutException)
            {
                logger.LogWarning("Database not reachable (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
