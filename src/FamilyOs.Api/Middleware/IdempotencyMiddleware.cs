using System.Collections.Concurrent;

namespace FamilyOs.Api.Middleware;

public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    private static readonly ConcurrentDictionary<string, (byte[] Body, int StatusCode, string ContentType)> Cache = new();

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues))
        {
            await next(context);
            return;
        }

        var userId = context.User.FindFirst("sub")?.Value ?? "anon";
        var key = $"{userId}:{keyValues.FirstOrDefault()}";

        if (Cache.TryGetValue(key, out var cached))
        {
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            await context.Response.Body.WriteAsync(cached.Body);
            return;
        }

        var originalBody = context.Response.Body;
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await next(context);

        ms.Position = 0;
        var body = ms.ToArray();
        Cache.TryAdd(key, (body, context.Response.StatusCode, context.Response.ContentType ?? "application/json"));

        context.Response.Body = originalBody;
        await originalBody.WriteAsync(body);
    }
}
