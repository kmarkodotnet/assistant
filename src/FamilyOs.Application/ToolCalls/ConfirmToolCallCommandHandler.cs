using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.ToolCalls;

/// <summary>api-design.md §16.3.1 — validate token → ITool.ExecuteAsync → explicit Approve audit.</summary>
public sealed class ConfirmToolCallCommandHandler(
    IToolCallTokenService tokenService,
    IToolRegistry registry,
    ICurrentUserAccessor currentUser,
    IAuditLogger auditLogger)
    : IRequestHandler<ConfirmToolCallCommand, ConfirmToolCallResultDto>
{
    private static readonly JsonSerializerOptions AuditJsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<ConfirmToolCallResultDto> Handle(ConfirmToolCallCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserAccountId is not { } userAccountId)
            throw new UnauthorizedException("Bejelentkezés szükséges.");

        var validation = tokenService.Validate(request.ProposalToken, userAccountId);
        if (!validation.Ok || validation.Envelope is not { } envelope)
        {
            throw validation.Error == ToolCallTokenError.UserMismatch
                ? new ForbiddenException("A javaslat nem az Ön munkamenetéhez tartozik.")
                : new UnauthorizedException("A javaslat lejárt vagy érvénytelen. Kérje újra a parancsot.");
        }

        if (!registry.TryGet(envelope.Tool, out var tool))
            throw new UnauthorizedException("Ismeretlen tool a javaslatban.");

        var ctx = new ToolExecutionContext(
            userAccountId, currentUser.FamilyMemberId, currentUser.Role ?? "Adult", DateTime.UtcNow, "Europe/Budapest");

        ToolResult result;
        try
        {
            result = await tool.ExecuteAsync(envelope.Args, ctx, cancellationToken);
        }
        catch (NotFoundException)
        {
            // The referenced entity existed at resolve time (the token was signed) but has
            // since disappeared — api-design.md §16.3.1 asks for 422 here, distinct from the
            // plain 404 the underlying command would otherwise raise for "this ID never existed".
            throw new DomainBusinessRuleException("A hivatkozott elem időközben törlésre került.");
        }

        await auditLogger.LogAsync(
            AuditAction.Approve,
            userAccountId,
            entityType: $"ToolCall:{tool.Name}",
            entityId: result.ResultId,
            detailsJson: JsonSerializer.Serialize(new { toolName = tool.Name, resolvedArgs = envelope.Args }, AuditJsonOpts),
            ct: cancellationToken);

        return new ConfirmToolCallResultDto(true, result.ResultType, result.ResultId, result.Summary);
    }
}
