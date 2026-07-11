using System.Globalization;
using System.Text;
using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Application.Search.Dtos;
using FamilyOs.Domain.Enums;

namespace FamilyOs.Application.Ai;

public enum ToolPlanOutcome { FallbackToQa, ParseFailed, ResolveFailed, Ready }

/// <summary>
/// Ready/ResolveFailed/ParseFailed carry a message (Hungarian) for the caller to surface as
/// SearchResponse.Answer; FallbackToQa means the caller should run the normal Q&amp;A/search flow.
/// </summary>
public sealed record ToolPlanResult(ToolPlanOutcome Outcome, ToolCallProposalDto? Proposal, string? Message);

/// <summary>
/// Builds the tool-catalog system prompt, calls IAiProvider.CompleteAsync, and runs the
/// robust strict-JSON parse (ai-pipeline.md §11.3) with a single corrective retry.
/// </summary>
public sealed class ToolCallPlanner(
    IAiProvider aiProvider,
    IToolRegistry registry,
    IToolCallTokenService tokenService,
    IAuditLogger auditLogger)
{
    private const string PromptId = "tool_call_planner";
    private const string PromptVersion = "v1";

    private static readonly JsonSerializerOptions AuditJsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<ToolPlanResult> PlanAsync(string query, ToolExecutionContext ctx, CancellationToken ct)
    {
        var systemPrompt = BuildSystemPrompt();

        var (parsed, error) = await TryCompleteAndParseAsync(systemPrompt, query, ct);

        if (parsed is null)
        {
            var correctiveUserPrompt =
                query + "\n\n[Az előző válaszod nem volt érvényes. Válaszolj KIZÁRÓLAG a fenti sémának " +
                "megfelelő érvényes JSON-nal, magyarázat nélkül.]";
            (parsed, error) = await TryCompleteAndParseAsync(systemPrompt, correctiveUserPrompt, ct);
        }

        if (parsed is null)
        {
            await auditLogger.LogAsync(
                AuditAction.AiCall,
                ctx.UserAccountId,
                entityType: "ToolCallPlanner",
                detailsJson: JsonSerializer.Serialize(new { success = false, error, promptId = PromptId }, AuditJsonOpts),
                ct: ct);

            return new ToolPlanResult(ToolPlanOutcome.ParseFailed, null,
                "Nem sikerült értelmeznem ezt utasításként. Fogalmazza meg másképp, vagy használja a megfelelő űrlapot.");
        }

        if (parsed.Action == "none")
            return new ToolPlanResult(ToolPlanOutcome.FallbackToQa, null, null);

        // parsed.Action == "tool_call" here — Tool/Arguments are guaranteed non-null by
        // TryCompleteAndParseAsync's validation, so this TryGet is expected to always
        // succeed; the check is kept to satisfy null-flow analysis and fail safely regardless.
        if (!registry.TryGet(parsed.Tool!, out var tool))
            return new ToolPlanResult(ToolPlanOutcome.ParseFailed, null, "Nem sikerült értelmeznem ezt utasításként.");

        var resolution = await tool.ResolveAsync(parsed.Arguments, ctx, ct);

        if (!resolution.Ok)
        {
            var message = resolution.Error ?? string.Join(" ", resolution.Warnings);
            return new ToolPlanResult(ToolPlanOutcome.ResolveFailed, null, message);
        }

        var (token, expiresUtc) = tokenService.CreateToken(tool.Name, resolution.ResolvedArguments, ctx.UserAccountId);
        var proposal = new ToolCallProposalDto(
            token,
            tool.Name,
            resolution.Summary,
            resolution.Display.Select(d => new ToolCallParameterDto(d.Label, d.Value)).ToList(),
            resolution.Warnings,
            expiresUtc);

        return new ToolPlanResult(ToolPlanOutcome.Ready, proposal, null);
    }

    private async Task<(ParsedToolCall? Parsed, string? Error)> TryCompleteAndParseAsync(
        string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var completion = await aiProvider.CompleteAsync(
            new AiPrompt(systemPrompt, userPrompt, PromptId, PromptVersion), ct);

        var block = JsonBlockExtractor.ExtractFirstObject(completion.Content);
        if (block is null)
            return (null, "no JSON object found in model output");

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(block);
        }
        catch (JsonException)
        {
            return (null, "invalid JSON");
        }

        using (doc)
        {
            var root = doc.RootElement;

            if (!root.TryGetProperty("action", out var actionEl) || actionEl.ValueKind != JsonValueKind.String)
                return (null, "missing 'action'");

            var action = actionEl.GetString();
            if (action == "none")
                return (new ParsedToolCall("none", null, default), null);

            if (action != "tool_call")
                return (null, $"unknown action '{action}'");

            if (!root.TryGetProperty("tool", out var toolEl) || toolEl.ValueKind != JsonValueKind.String)
                return (null, "missing 'tool'");

            var toolName = toolEl.GetString()!;
            if (!registry.TryGet(toolName, out var tool))
                return (null, $"unknown tool '{toolName}'");

            if (!root.TryGetProperty("arguments", out var argsEl) || argsEl.ValueKind != JsonValueKind.Object)
                return (null, "missing 'arguments'");

            if (!JsonSchemaLiteValidator.TryValidate(tool.JsonSchema, argsEl, out var schemaError))
                return (null, schemaError);

            // Clone: argsEl is backed by `doc`, which is disposed at the end of this block.
            return (new ParsedToolCall("tool_call", toolName, argsEl.Clone()), null);
        }
    }

    private string BuildSystemPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Te a Family OS asszisztense vagy. A felhasználó egy magyar nyelvű üzenetet ír.");
        sb.AppendLine("Ha az üzenet egy VÉGREHAJTHATÓ UTASÍTÁS az alábbi tool-katalógusból, javasolj egy tool-hívást.");
        sb.AppendLine("Ha az üzenet kérdés, keresés vagy nem illik egyik tool-ra sem, ne javasolj tool-hívást.");
        sb.AppendLine();
        sb.AppendLine("Elérhető tool-ok:");

        foreach (var tool in registry.All)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"- \"{tool.Name}\": {tool.Description}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  JSON Schema: {tool.JsonSchema.GetRawText()}");
        }

        sb.AppendLine();
        sb.AppendLine("A válaszod KIZÁRÓLAG egyetlen JSON objektum legyen, más szöveg nélkül. Formátum tool-hívásnál:");
        sb.AppendLine("""{"action":"tool_call","tool":"<név>","arguments":{...},"userConfirmationText":"<magyar megerősítő kérdés>"}""");
        sb.AppendLine("Ha nincs illeszkedő szándék:");
        sb.AppendLine("""{"action":"none","reason":"<magyar indoklás>"}""");

        return sb.ToString();
    }

    private sealed record ParsedToolCall(string Action, string? Tool, JsonElement Arguments);
}
