using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Api.Contracts;

namespace SchoolAdmission.Tests.Integration;

public abstract class ApiTestBase : IAsyncLifetime
{
    protected ApiFactory Factory { get; } = new();
    protected HttpClient Admin { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await ((IAsyncLifetime)Factory).InitializeAsync();
        Admin = Factory.CreateClient();
    }

    public Task DisposeAsync() => ((IAsyncLifetime)Factory).DisposeAsync();

    /// <summary>Deterministic, valid 17-digit student id for test number <paramref name="n"/>.</summary>
    protected static string Student(int n) => $"2015{n:D13}";

    protected static async Task<ClassResponse> CreateClassAsync(HttpClient client, int seats, string? name = null)
    {
        var response = await client.PostAsJsonAsync("/classes",
            new { name = name ?? $"Class {Guid.NewGuid():N}", totalSeats = seats });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClassResponse>())!;
    }

    protected static async Task<ClassResponse> GetClassAsync(HttpClient client, Guid classId) =>
        (await client.GetFromJsonAsync<ClassResponse>($"/classes/{classId}"))!;

    protected static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
