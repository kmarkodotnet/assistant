using FamilyOs.Api.Auth;
using FamilyOs.Api.Endpoints;
using FamilyOs.Api.Middleware;
using FamilyOs.Api.Realtime;
using FamilyOs.Application;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Infrastructure;
using FamilyOs.Infrastructure.Ai.DependencyInjection;
using FamilyOs.Infrastructure.Health;
using FamilyOs.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Globalization;
using System.Text.Json.Serialization;

// Bootstrap logger (no IFormatProvider overload in CreateBootstrapLogger; use formatProvider in WriteTo)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// Services
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFamilyOsAiServices(builder.Configuration);
builder.Services.AddAuthorization(opts => opts.AddFamilyOsPolicies());

// HttpClient for OllamaHealthCheck
builder.Services.AddHttpClient("ollama", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Ollama:BaseUrl"] ?? "http://ollama:11434");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var connStr =
    builder.Configuration.GetConnectionString("DefaultConnection") is { Length: > 0 } cs
        ? cs
        : builder.Configuration.GetConnectionString("Default") ?? string.Empty;

var hcBuilder = builder.Services.AddHealthChecks()
    .AddCheck<OllamaHealthCheck>("ollama", HealthStatus.Degraded, tags: ["ready"]);

if (!string.IsNullOrWhiteSpace(connStr))
{
    hcBuilder.AddNpgSql(connStr, name: "postgres",
        failureStatus: HealthStatus.Unhealthy, tags: ["ready"]);
}

builder.Services.AddSignalR();
builder.Services.AddSingleton<IProcessingProgressNotifier, SignalRProgressNotifier>();

// Override NullNotificationPusher with real SignalR implementation
builder.Services.AddScoped<IInAppNotificationPusher, SignalRNotificationPusher>();

builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Run migrations and sync DB role passwords before accepting traffic
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DbSeedRunner>().RunAsync();
}

// Trust X-Forwarded-Proto from nginx so the app knows it's behind HTTPS
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});

// Middleware pipeline (order critical)
app.UseMiddleware<ExceptionToProblemDetailsMiddleware>();
app.UseMiddleware<TraceIdEnrichmentMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Health endpoints
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = (ctx, _) =>
    {
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsync("{\"status\":\"live\"}");
    },
});
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains("ready"),
});

// API endpoints
app.MapAuthEndpoints();
app.MapFamilyEndpoints();
app.MapUsersEndpoints();
app.MapDocumentEndpoints();
app.MapSearchEndpoints();
app.MapTasksEndpoints();
app.MapDeadlinesEndpoints();
app.MapSuggestionsEndpoints();
app.MapRemindersEndpoints();
app.MapNotificationsEndpoints();
app.MapNotesEndpoints();
app.MapTagsEndpoints();
app.MapTopicsEndpoints();
app.MapDashboardEndpoints();
app.MapAuditEndpoints();
app.MapAiJobsAdminEndpoints();
app.MapAiProvidersAdminEndpoints();
app.MapSourcesEndpoints();
app.MapSettingsEndpoints();

// SignalR hubs
app.MapHub<DocumentsHub>("/hubs/documents");
app.MapHub<NotificationsHub>("/hubs/notifications");

// System endpoints
app.MapGet("/api/v1/system/heartbeat",
    () => Results.Ok(new { ok = true, serverTimeUtc = DateTime.UtcNow }));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.RunAsync();

public partial class Program { }
