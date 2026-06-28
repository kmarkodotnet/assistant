using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Domain.Enums;
using MediatR;
using System.Reflection;

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
            var n when n.Contains("Create") || n.Contains("Upload") => AuditAction.Create,
            var n when n.Contains("Delete") => AuditAction.Delete,
            var n when n.Contains("Patch") || n.Contains("Update") => AuditAction.Update,
            var n when n.Contains("Approve") => AuditAction.Approve,
            var n when n.Contains("Reject") => AuditAction.Reject,
            _ => AuditAction.Update,
        };

        var idProperty = requestType.GetProperty("Id");
        Guid? entityId = null;
        if (idProperty is not null && idProperty.PropertyType == typeof(Guid))
            entityId = (Guid)idProperty.GetValue(request)!;

        await auditLogger.LogAsync(
            action,
            currentUser.UserAccountId,
            requestType.Name,
            entityId,
            ct: cancellationToken);

        return response;
    }
}
