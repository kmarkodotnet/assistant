using FamilyOs.Application.Common.Behaviors;
using MediatR;

namespace FamilyOs.Application.ToolCalls;

// [NoAudit]: same reasoning as ConfirmToolCallCommand — the entityType needs the
// "ToolCall:<toolName>" shape (ADR-0011 D4), which the handler writes explicitly.
[NoAudit]
public sealed record RejectToolCallCommand(string ProposalToken, string? Reason) : IRequest;
