using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Application;
using SchoolAdmission.Api.Http;
using SchoolAdmission.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration (all overridable through environment variables) ----
builder.Services.AddOptions<HoldOptions>()
    .Configure<IConfiguration>((o, config) => o.DurationSeconds = config.GetValue(HoldOptions.DurationKey, o.DurationSeconds))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ---- Services ----
builder.Services.AddDbContext<AdmissionDbContext>((sp, o) => o.UseNpgsql(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing (env: ConnectionStrings__Default).")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IHoldService, HoldService>();

builder.Services.AddControllers()
    // The web defaults accept numbers sent as strings ("totalSeats": "10"); the contract says number.
    .AddJsonOptions(o => o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict)
    .ConfigureApiBehaviorOptions(o =>
    {
        // Give model-validation failures the same machine-readable "code" as domain errors.
        var defaultFactory = o.InvalidModelStateResponseFactory;
        o.InvalidModelStateResponseFactory = context =>
        {
            var result = defaultFactory(context);
            if (result is ObjectResult { Value: ProblemDetails problem })
            {
                problem.Extensions["code"] = "VALIDATION_FAILED";
            }
            return result;
        };
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<AdmissionDbContext>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length == 0) policy.AllowAnyOrigin();
    else policy.WithOrigins(allowedOrigins);

    // "Date" lets the UI correct for browser clock skew when counting down to expiresAt.
    policy.AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Date", "Location");
}));

var app = builder.Build();

// ---- Pipeline ----
app.UseExceptionHandler();
app.UseStatusCodePages(); // problem+json for bare 404/405s, e.g. a malformed id in the route
app.UseCors();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapControllers();

await app.MigrateDatabaseAsync();
await app.RunAsync();

public partial class Program; // exposed for WebApplicationFactory in tests
