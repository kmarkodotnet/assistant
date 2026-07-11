using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.ToolCalls;

/// <summary>
/// api-design.md §16.3.2 — no execution, ever. Best-effort: even a malformed/expired token
/// still produces a Reject audit entry (the client may also just discard the card client-side
/// without calling this at all — this endpoint exists for the *intentional, logged* rejection).
/// </summary>
public sealed class RejectToolCallCommandHandler(
    IToolCallTokenService tokenService,
    ICurrentUserAccessor currentUser,
    IAuditLogger auditLogger)
    : IRequestHandler<RejectToolCallCommand>
{
    private static readonly JsonSerializerOptions AuditJsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task Handle(RejectToolCallCommand request, CancellationToken cancellationToken)
    {
        var userAccountId = currentUser.UserAccountId;
        var validation = tokenService.Validate(request.ProposalToken, userAccountId ?? Guid.Empty);
        var toolName = validation is { Ok: true, Envelope: not null } ? validation.Envelope.Tool : "unknown";

        await auditLogger.LogAsync(
            AuditAction.Reject,
            userAccountId,
            entityType: $"ToolCall:{toolName}",
            detailsJson: JsonSerializer.Serialize(new { reason = request.Reason }, AuditJsonOpts),
            ct: cancellationToken);
    }
}
