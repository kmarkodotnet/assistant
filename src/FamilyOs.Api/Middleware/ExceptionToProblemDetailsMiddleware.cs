using FamilyOs.Application.Common.Errors;
using System.Diagnostics;
using System.Text.Json;

namespace FamilyOs.Api.Middleware;

public sealed class ExceptionToProblemDetailsMiddleware(
    RequestDelegate next,
    ILogger<ExceptionToProblemDetailsMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly Action<ILogger, string, string, Exception?> LogDomainWarning =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1, "DomainException"),
            "Domain exception [{ExceptionType}]: {Message}");

    private static readonly Action<ILogger, string, Exception?> LogInfraError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, "InfrastructureException"),
            "Infrastructure exception [{ExceptionType}]");

    private static readonly Action<ILogger, string, Exception?> LogUnhandledError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, "UnhandledException"),
            "Unhandled exception [{ExceptionType}]");

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        int statusCode;
        string type;
        string title;
        string detail;
        IReadOnlyDictionary<string, string[]>? fieldErrors = null;

        switch (ex)
        {
            case ValidationException vex:
                statusCode = StatusCodes.Status400BadRequest;
                type = "https://family-os.local/errors/validation";
                title = "Érvénytelen kérés";
                detail = vex.UserMessage;
                fieldErrors = vex.Errors;
                LogDomainWarning(logger, ex.GetType().Name, vex.Message, null);
                break;
            case NotFoundException nex:
                statusCode = StatusCodes.Status404NotFound;
                type = "https://family-os.local/errors/not-found";
                title = "Nem található";
                detail = nex.UserMessage;
                LogDomainWarning(logger, ex.GetType().Name, nex.Message, null);
                break;
            case ConflictException cex:
                statusCode = StatusCodes.Status409Conflict;
                type = "https://family-os.local/errors/conflict";
                title = "Ütközés";
                detail = cex.UserMessage;
                LogDomainWarning(logger, ex.GetType().Name, cex.Message, null);
                break;
            case ForbiddenException fex:
                statusCode = StatusCodes.Status403Forbidden;
                type = "https://family-os.local/errors/forbidden";
                title = "Hozzáférés megtagadva";
                detail = fex.UserMessage;
                LogDomainWarning(logger, ex.GetType().Name, fex.Message, null);
                break;
            case UnsupportedMediaException umex:
                statusCode = StatusCodes.Status415UnsupportedMediaType;
                type = "https://family-os.local/errors/unsupported-media";
                title = "Nem támogatott formátum";
                detail = umex.UserMessage;
                LogDomainWarning(logger, ex.GetType().Name, umex.Message, null);
                break;
            case DomainBusinessRuleException bex:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                type = "https://family-os.local/errors/business-rule";
                title = "Üzleti szabály megsértése";
                detail = bex.UserMessage;
                LogDomainWarning(logger, ex.GetType().Name, bex.Message, null);
                break;
            case InfrastructureException:
                statusCode = StatusCodes.Status503ServiceUnavailable;
                type = "https://family-os.local/errors/service-unavailable";
                title = "Szolgáltatás nem elérhető";
                detail = "A szolgáltatás átmenetileg nem elérhető.";
                LogInfraError(logger, ex.GetType().Name, ex);
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                type = "https://family-os.local/errors/internal-error";
                title = "Belső hiba";
                detail = "Belső szerverhiba történt.";
                LogUnhandledError(logger, ex.GetType().Name, ex);
                break;
        }

        var problem = new Dictionary<string, object?>
        {
            ["type"] = type,
            ["title"] = title,
            ["status"] = statusCode,
            ["detail"] = detail,
            ["traceId"] = traceId,
        };

        if (fieldErrors is not null)
            problem["fieldErrors"] = fieldErrors;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
