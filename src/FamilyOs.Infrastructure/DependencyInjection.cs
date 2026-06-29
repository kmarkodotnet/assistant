using FamilyOs.Application.Abstractions.Auth;
using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Abstractions.Storage;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Application.Common.Ai;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Documents.Common;
using FamilyOs.Application.Notes.Common;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Audit;
using FamilyOs.Infrastructure.Auth;
using FamilyOs.Infrastructure.Authorization;
using FamilyOs.Infrastructure.Common;
using FamilyOs.Infrastructure.Documents;
using FamilyOs.Infrastructure.Markdown;
using FamilyOs.Infrastructure.Notifications;
using FamilyOs.Infrastructure.Persistence;
using FamilyOs.Infrastructure.Persistence.Repositories;
using FamilyOs.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.NameTranslation;

namespace FamilyOs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserService>();

        var connectionString = configuration.GetConnectionString("Default")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? string.Empty;

        // DB enums are stored as PascalCase (e.g. 'Self', 'Admin'). The default Npgsql
        // translator would send snake_case ('self', 'admin'), causing 22P02 errors.
        var pgNameTranslator = new NpgsqlNullNameTranslator();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<UserRole>("app.user_role", nameTranslator: pgNameTranslator);
        dataSourceBuilder.MapEnum<Relation>("app.relation", nameTranslator: pgNameTranslator);
        dataSourceBuilder.MapEnum<AuditAction>("app.audit_action", nameTranslator: pgNameTranslator);
        dataSourceBuilder.MapEnum<ProcessingStatus>("app.processing_status", nameTranslator: pgNameTranslator);
        dataSourceBuilder.MapEnum<SourceType>("app.source_type", nameTranslator: pgNameTranslator);
        dataSourceBuilder.MapEnum<Origin>("app.origin", nameTranslator: pgNameTranslator);
        dataSourceBuilder.MapEnum<ExtractionMethod>("app.extraction_method", nameTranslator: pgNameTranslator);
        dataSourceBuilder.MapEnum<MedicalRecordType>("app.medical_record_type", nameTranslator: pgNameTranslator);
        dataSourceBuilder.MapEnum<FinancialRecordType>("app.financial_record_type", nameTranslator: pgNameTranslator);
        dataSourceBuilder.MapEnum<RecurrencePeriod>("app.recurrence_period", nameTranslator: pgNameTranslator);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        services.AddDbContext<FamilyOsDbContext>(opts =>
            opts.UseNpgsql(dataSource, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "app");
                    npgsql.UseVector();
                })
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IFamilyOsDbContext>(sp => sp.GetRequiredService<FamilyOsDbContext>());

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-family-os-session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Events.OnValidatePrincipal = RevokedSessionChecker.ValidatePrincipalAsync;
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };
            });

        services.AddSingleton<IAllowlistService, AllowlistService>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        // Authorization
        services.AddScoped<IFamilyOsAuthorizationService, FamilyOsAuthorizationService>();

        // Audit
        services.AddScoped<IAuditLogger, DbAuditLogger>();

        // Storage
        services.AddScoped<IDocumentStorage, LocalFilesystemDocumentStorage>();

        // MIME detection
        services.AddSingleton<IMimeDetector, MimeDetector>();

        // Document helpers
        services.AddScoped<IDuplicateDocumentChecker, DuplicateDocumentChecker>();

        // AI job repository
        services.AddScoped<IAiProcessingJobRepository, AiProcessingJobRepository>();

        // Notifications
        services.Configure<SmtpOptions>(configuration.GetSection("Notifications:Smtp"));
        services.AddScoped<InAppNotificationService>();
        services.AddScoped<SmtpNotificationService>();
        services.AddScoped<INotificationService, CompositeNotificationService>();

        // Null pusher by default (overridden in API project with SignalR implementation)
        services.AddScoped<IInAppNotificationPusher, NullNotificationPusher>();

        // Markdown
        services.AddScoped<IMarkdownSanitizer, MarkdownSanitizer>();

        return services;
    }
}
