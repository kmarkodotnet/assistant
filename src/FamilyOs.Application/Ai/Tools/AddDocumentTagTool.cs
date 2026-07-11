using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Documents.AddDocumentTag;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Ai.Tools;

/// <summary>add_tag — maps to the new AddDocumentTagCommand (ADR-0011 D3). Only existing tags.</summary>
public sealed class AddDocumentTagTool(
    ISender sender,
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService) : ITool
{
    public string Name => "add_tag";

    public string Description => "Egy meglévő címkét hozzáad egy dokumentumhoz.";

    public JsonElement JsonSchema { get; } = JsonDocument.Parse("""
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": "object",
          "additionalProperties": false,
          "required": ["documentRef", "tagName"],
          "properties": {
            "documentRef": { "type": "string", "minLength": 2, "maxLength": 200 },
            "tagName":     { "type": "string", "minLength": 1, "maxLength": 60 }
          }
        }
        """).RootElement.Clone();

    public async Task<ToolResolution> ResolveAsync(JsonElement rawArguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var documentRef = rawArguments.GetProperty("documentRef").GetString()!;
        var tagName = rawArguments.GetProperty("tagName").GetString()!;

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

        // Case-insensitive exact match only — MVP-ben az LLM nem hoz létre új tag-et
        // (ai-pipeline.md §11.2).
        var tag = await db.Tags.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == tagName.ToLowerInvariant().Trim(), ct);

        if (tag is null)
            return ToolResolution.Failure($"Nem létezik ilyen címke: \"{tagName}\".");

        var resolvedJson = $$"""
            { "documentId": "{{document.Id}}", "tagId": "{{tag.Id}}" }
            """;
        using var resolvedDoc = JsonDocument.Parse(resolvedJson);

        var summary = $"A(z) \"{tag.Name}\" címkét hozzáadom a(z) \"{document.Title}\" dokumentumhoz.";
        var display = new List<ToolParamDisplay>
        {
            new("Dokumentum", document.Title),
            new("Címke", tag.Name),
        };

        return new ToolResolution(true, resolvedDoc.RootElement.Clone(), summary, display, [], null);
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement resolvedArguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var documentId = resolvedArguments.GetProperty("documentId").GetGuid();
        var tagId = resolvedArguments.GetProperty("tagId").GetGuid();

        await sender.Send(new AddDocumentTagCommand(documentId, tagId), ct);

        return new ToolResult("Document", documentId, "A címke hozzáadása megtörtént.");
    }
}
