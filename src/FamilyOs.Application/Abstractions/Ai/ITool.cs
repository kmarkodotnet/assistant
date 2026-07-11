using System.Text.Json;

namespace FamilyOs.Application.Abstractions.Ai;

/// <summary>
/// A whitelisted tool the LLM may propose. Contract: ai-pipeline.md §11.1 / ADR-0011.
/// The tool NEVER writes data itself — ExecuteAsync only dispatches MediatR commands via
/// ISender, so the existing AuditBehavior auto-logs the underlying business effect.
/// </summary>
public interface ITool
{
    /// <summary>Stable, whitelisted identifier; goes into the prompt and the proposal token.</summary>
    string Name { get; }

    /// <summary>Hungarian description for the prompt's tool catalog.</summary>
    string Description { get; }

    /// <summary>JSON Schema (draft 2020-12) for the raw LLM arguments.</summary>
    JsonElement JsonSchema { get; }

    /// <summary>
    /// Phase 1 (search/Command time): validates and resolves names/relative dates into
    /// concrete IDs and absolute UTC instants. Never writes data. Never throws for
    /// business-level ambiguity/not-found — reports via ToolResolution.Ok=false instead.
    /// </summary>
    Task<ToolResolution> ResolveAsync(
        JsonElement rawArguments, ToolExecutionContext ctx, CancellationToken ct);

    /// <summary>
    /// Phase 2 (confirm time): builds MediatR command(s) from the resolved arguments and
    /// executes them via ISender.Send. All writes happen here.
    /// </summary>
    Task<ToolResult> ExecuteAsync(
        JsonElement resolvedArguments, ToolExecutionContext ctx, CancellationToken ct);
}

/// <summary>
/// Snapshot of the calling user (from ICurrentUserAccessor); tools use this for
/// TargetUserAccountId/CreatedBy defaults and as the base for relative-date resolution.
/// </summary>
public sealed record ToolExecutionContext(
    Guid UserAccountId, Guid? FamilyMemberId, string Role,
    DateTime NowUtc, string TimeZoneId);

public sealed record ToolResolution(
    bool Ok,
    JsonElement ResolvedArguments, // concrete IDs + absolute UTC instants
    string Summary,                // Hungarian, human confirmation text
    IReadOnlyList<ToolParamDisplay> Display, // label/value pairs for the confirmation card
    IReadOnlyList<string> Warnings,
    string? Error) // Hungarian error message if !Ok
{
    public static ToolResolution Failure(string error, IReadOnlyList<string>? warnings = null) =>
        new(Ok: false,
            ResolvedArguments: default,
            Summary: string.Empty,
            Display: [],
            Warnings: warnings ?? [error],
            Error: error);
}

public sealed record ToolParamDisplay(string Label, string Value);

public sealed record ToolResult(
    string ResultType, // e.g. "Reminder"
    Guid ResultId,
    string Summary); // Hungarian confirmation text
