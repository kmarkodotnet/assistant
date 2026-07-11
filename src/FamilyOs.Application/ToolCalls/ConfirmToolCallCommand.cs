using FamilyOs.Application.Common.Behaviors;
using MediatR;

namespace FamilyOs.Application.ToolCalls;

public sealed record ConfirmToolCallResultDto(bool Executed, string ResultType, Guid ResultId, string Summary);

// [NoAudit]: the generic AuditBehavior can't produce the "ToolCall:<toolName>" entityType
// this command needs (ADR-0011 D4) — the handler writes an explicit Approve audit entry instead.
// The underlying business command(s) dispatched from ITool.ExecuteAsync still go through
// ISender and get their own normal AuditBehavior entries (Create/Update).
[NoAudit]
public sealed record ConfirmToolCallCommand(string ProposalToken) : IRequest<ConfirmToolCallResultDto>;
