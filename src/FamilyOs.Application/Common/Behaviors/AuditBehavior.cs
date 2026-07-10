using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Domain.Enums;
using MediatR;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FamilyOs.Application.Common.Behaviors;

public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditLogger auditLogger,
    ICurrentUserAccessor currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly HashSet<string> _skipExact = new(StringComparer.Ordinal)
    {
        "LoginGoogleCommand",
        "LogoutCommand",
    };

    // Properties identifying the acting user — already stored as userAccountId
    private static readonly HashSet<string> _actorProps = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedByUserId", "CreatedByUserAccountId",
        "RequestingUserId", "ApprovedByUserId",
        "UserId", "UserAccountId",
    };

    // Large content or secret fields — excluded from details JSON
    private static readonly HashSet<string> _excludedFromDetails = new(StringComparer.OrdinalIgnoreCase)
    {
        "Body", "Content", "RefreshToken", "Token", "Password", "Secret", "RowVersion",
    };

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);

        if (requestType.GetCustomAttribute<NoAuditAttribute>() is not null)
            return await next();

        var name = requestType.Name;

        if (!name.EndsWith("Command", StringComparison.Ordinal) || _skipExact.Contains(name))
            return await next();

        var response = await next();

        var action = name switch
        {
            var n when n.Contains("Create") || n.Contains("Upload") || n.Contains("Connect") => AuditAction.Create,
            var n when n.Contains("Delete") || n.Contains("Remove") => AuditAction.Delete,
            var n when n.Contains("Patch") || n.Contains("Update") => AuditAction.Update,
            var n when n.Contains("Approve") || n.Contains("Complete") || n.Contains("Resolve") || n.Contains("Acknowledge") => AuditAction.Approve,
            var n when n.Contains("Reject") || n.Contains("Dismiss") || n.Contains("Cancel") || n.Contains("Skip") => AuditAction.Reject,
            _ => AuditAction.Update,
        };

        var entityId = ExtractEntityId(requestType, request, response);
        var detailsJson = BuildDetailsJson(requestType, request);

        await auditLogger.LogAsync(
            action,
            currentUser.UserAccountId,
            requestType.Name,
            entityId,
            detailsJson: detailsJson,
            ct: cancellationToken);

        return response;
    }

    private static Guid? ExtractEntityId(Type requestType, object request, object? response)
    {
        // 1. Exact "Id" property (non-nullable)
        var idProp = requestType.GetProperty("Id");
        if (idProp?.PropertyType == typeof(Guid))
            return (Guid)idProp.GetValue(request)!;

        // 2. First non-nullable *Id Guid property that isn't an actor property.
        //    We only take non-nullable Guids here — nullable ones (AssignedToFamilyMemberId,
        //    RelatedFamilyMemberId, ParentId, etc.) are secondary references, not the primary entity.
        foreach (var p in requestType.GetProperties())
        {
            if (_actorProps.Contains(p.Name)) continue;
            if (!p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)) continue;
            if (p.PropertyType != typeof(Guid)) continue;

            return (Guid)p.GetValue(request)!;
        }

        // 3. Extract from the response DTO (covers Create commands where the ID is generated)
        if (response is not null)
        {
            var responseIdProp = response.GetType().GetProperty("Id");
            if (responseIdProp?.PropertyType == typeof(Guid))
                return (Guid)responseIdProp.GetValue(response)!;
        }

        return null;
    }

    private static string? BuildDetailsJson(Type requestType, object request)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var p in requestType.GetProperties())
        {
            if (_actorProps.Contains(p.Name)) continue;
            if (_excludedFromDetails.Contains(p.Name)) continue;
            if (p.PropertyType.IsAssignableTo(typeof(Stream))) continue;
            if (p.PropertyType == typeof(byte[])) continue;

            var value = p.GetValue(request);
            if (value is null) continue;

            dict[p.Name] = value;
        }

        return dict.Count == 0 ? null : JsonSerializer.Serialize(dict, _jsonOpts);
    }
}
