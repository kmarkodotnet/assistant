using System.Diagnostics;

namespace FamilyOs.Api.Middleware;

public sealed class TraceIdEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        context.Response.Headers["X-Request-Id"] = traceId;
        await next(context);
    }
}
