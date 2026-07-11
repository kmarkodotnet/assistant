using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Documents.PatchDocument;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Ai.Tools;

/// <summary>assign_document — maps to PatchDocumentCommand (RelatedFamilyMemberId only).</summary>
public sealed class AssignDocumentTool(
    ISender sender,
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService) : ITool
{
    public string Name => "assign_document";

    public string Description => "Egy dokumentumot egy családtaghoz rendel.";

    public JsonElement JsonSchema { get; } = JsonDocument.Parse("""
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": "object",
          "additionalProperties": false,
          "required": ["documentRef", "familyMemberRef"],
          "properties": {
            "documentRef":     { "type": "string", "minLength": 2, "maxLength": 200 },
            "familyMemberRef": { "type": "string", "minLength": 1, "maxLength": 100 }
          }
        }
        """).RootElement.Clone();

    public async Task<ToolResolution> ResolveAsync(JsonElement rawArguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var documentRef = rawArguments.GetProperty("documentRef").GetString()!;
        var familyMemberRef = rawArguments.GetProperty("familyMemberRef").GetString()!;

        var documentCandidates = await db.Documents.AsNoTracking().ToListAsync(ct);
        var visibleDocuments = documentCandidates.Where(authService.CanReadDocument).ToList();
        var (docOutcome, document) = RefMatcher.Match(visibleDocuments, documentRef, d => d.Title);

        if (docOutcome != RefMatchOutcome.Found || document is null)
        {
            var error = docOutcome == RefMatchOutcome.Ambiguous
                ? $"Több dokumentum is illeszkedik erre: \"{documentRef}\". Kérem, pontosítsa."
                : $"Nem található dokumentum ezzel a névvel: \"{documentRef}\".";
            return ToolResolution.Failure(error);
        }

        var memberCandidates = await db.FamilyMembers.AsNoTracking().ToListAsync(ct);
        var (memberOutcome, member) = RefMatcher.Match(
            memberCandidates, familyMemberRef, m => m.FullName is { Length: > 0 } fn ? fn : m.DisplayName);

        // Fall back to matching by DisplayName alone if FullName-based matching found nothing —
        // most family members only have a DisplayName set (FullName is optional).
        if (memberOutcome != RefMatchOutcome.Found)
            (memberOutcome, member) = RefMatcher.Match(memberCandidates, familyMemberRef, m => m.DisplayName);

        if (memberOutcome != RefMatchOutcome.Found || member is null)
        {
            var error = memberOutcome == RefMatchOutcome.Ambiguous
                ? $"Több családtag is illeszkedik erre: \"{familyMemberRef}\". Kérem, pontosítsa."
                : $"Nem található családtag ezzel a névvel: \"{familyMemberRef}\".";
            return ToolResolution.Failure(error);
        }

        var resolvedJson = $$"""
            { "documentId": "{{document.Id}}", "relatedFamilyMemberId": "{{member.Id}}" }
            """;
        using var resolvedDoc = JsonDocument.Parse(resolvedJson);

        var summary = $"A(z) \"{document.Title}\" dokumentumot {member.DisplayName} nevéhez rendelem.";
        var display = new List<ToolParamDisplay>
        {
            new("Dokumentum", document.Title),
            new("Családtag", member.DisplayName),
        };

        return new ToolResolution(true, resolvedDoc.RootElement.Clone(), summary, display, [], null);
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement resolvedArguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var documentId = resolvedArguments.GetProperty("documentId").GetGuid();
        var relatedFamilyMemberId = resolvedArguments.GetProperty("relatedFamilyMemberId").GetGuid();

        // RowVersion is intentionally null: PatchDocumentCommandHandler relies on EF Core's
        // shadow "xmin" concurrency token during SaveChangesAsync (catches
        // DbUpdateConcurrencyException → 409), it does not read cmd.RowVersion at all — so
        // there is nothing to "re-read at execute time" here despite the token not carrying it.
        await sender.Send(new PatchDocumentCommand(documentId, null, null, relatedFamilyMemberId, null, null), ct);

        return new ToolResult("Document", documentId, "A dokumentum hozzárendelése megtörtént.");
    }
}
