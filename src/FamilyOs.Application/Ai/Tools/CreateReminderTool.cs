using System.Globalization;
using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Application.Deadlines;
using FamilyOs.Application.Reminders;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Ai.Tools;

/// <summary>
/// create_reminder — maps to CreateReminderCommand; for a warranty anchor, chains
/// CreateDeadlineCommand → CreateReminderCommand behind a single confirmation (ADR-0011 D2).
/// </summary>
public sealed class CreateReminderTool(
    ISender sender,
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService) : ITool
{
    public string Name => "create_reminder";

    public string Description =>
        "Emlékeztetőt hoz létre egy feladat, határidő vagy termékgarancia lejárata előtt N nappal.";

    public JsonElement JsonSchema { get; } = JsonDocument.Parse("""
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": "object",
          "additionalProperties": false,
          "required": ["anchorType", "anchorRef", "offsetDays"],
          "properties": {
            "anchorType": { "enum": ["task", "deadline", "warranty"] },
            "anchorRef":  { "type": "string", "minLength": 2, "maxLength": 200,
                            "description": "A feladat/határidő címe vagy a termék neve." },
            "offsetDays": { "type": "integer", "minimum": 0, "maximum": 365,
                            "description": "Ennyi nappal a horgony dátuma ELŐTT; 0 = aznap." },
            "channel":    { "enum": ["inApp", "email"], "default": "inApp" },
            "recurrence": { "type": ["string", "null"], "default": null,
                            "description": "RRULE, általában null." }
          }
        }
        """).RootElement.Clone();

    public async Task<ToolResolution> ResolveAsync(JsonElement rawArguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var anchorType = rawArguments.GetProperty("anchorType").GetString()!;
        var anchorRef = rawArguments.GetProperty("anchorRef").GetString()!;
        var offsetDays = rawArguments.GetProperty("offsetDays").GetInt32();
        var channel = rawArguments.TryGetProperty("channel", out var chEl) && chEl.ValueKind == JsonValueKind.String
            ? chEl.GetString()!
            : "inApp";
        var recurrence = rawArguments.TryGetProperty("recurrence", out var recEl) && recEl.ValueKind == JsonValueKind.String
            ? recEl.GetString()
            : null;

        return anchorType switch
        {
            "task" => await ResolveTaskAsync(anchorRef, offsetDays, channel, recurrence, ctx, ct),
            "deadline" => await ResolveDeadlineAsync(anchorRef, offsetDays, channel, recurrence, ctx, ct),
            "warranty" => await ResolveWarrantyAsync(anchorRef, offsetDays, channel, recurrence, ctx, ct),
            _ => ToolResolution.Failure($"Ismeretlen horgonytípus: {anchorType}."),
        };
    }

    private async Task<ToolResolution> ResolveTaskAsync(
        string anchorRef, int offsetDays, string channel, string? recurrence, ToolExecutionContext ctx, CancellationToken ct)
    {
        var candidates = await db.Tasks.AsNoTracking().Where(t => t.DueDateUtc != null).ToListAsync(ct);
        // Same visibility scoping as the warranty branch below and AssignDocumentTool/
        // AddDocumentTagTool — without this, a title-only match would let one user create a
        // reminder (and leak the title+date in the confirmation card) for another user's
        // private task (code review finding on c43dd87).
        var visible = candidates.Where(authService.CanReadTask).ToList();
        var (outcome, task) = RefMatcher.Match(visible, anchorRef, t => t.Title);

        if (outcome != RefMatchOutcome.Found || task is null)
            return NotFoundOrAmbiguous(outcome, "feladatot", anchorRef);

        var triggerUtc = task.DueDateUtc!.Value.AddDays(-offsetDays);

        var resolved = BuildResolvedArgs(new
        {
            anchorType = "task",
            taskId = task.Id,
            triggerUtc,
            channel,
            recurrence,
            targetUserAccountId = ctx.UserAccountId,
            createdByUserId = ctx.UserAccountId,
        });

        var summary = $"Emlékeztetőt hozok létre a(z) \"{task.Title}\" feladathoz, {offsetDays} nappal a határidő előtt ({FormatLocal(triggerUtc)}).";
        var display = new List<ToolParamDisplay>
        {
            new("Feladat", task.Title),
            new("Emlékeztető", FormatLocal(triggerUtc)),
            new("Csatorna", DisplayChannel(channel)),
        };

        return new ToolResolution(true, resolved, summary, display, [], null);
    }

    private async Task<ToolResolution> ResolveDeadlineAsync(
        string anchorRef, int offsetDays, string channel, string? recurrence, ToolExecutionContext ctx, CancellationToken ct)
    {
        var candidates = await db.Deadlines.AsNoTracking().ToListAsync(ct);
        // See ResolveTaskAsync above for why this filter is required.
        var visible = candidates.Where(authService.CanReadDeadline).ToList();
        var (outcome, deadline) = RefMatcher.Match(visible, anchorRef, d => d.Title);

        if (outcome != RefMatchOutcome.Found || deadline is null)
            return NotFoundOrAmbiguous(outcome, "határidőt", anchorRef);

        var triggerUtc = deadline.DueDateUtc.AddDays(-offsetDays);

        var resolved = BuildResolvedArgs(new
        {
            anchorType = "deadline",
            deadlineId = deadline.Id,
            triggerUtc,
            channel,
            recurrence,
            targetUserAccountId = ctx.UserAccountId,
            createdByUserId = ctx.UserAccountId,
        });

        var summary = $"Emlékeztetőt hozok létre a(z) \"{deadline.Title}\" határidőhöz, {offsetDays} nappal előtte ({FormatLocal(triggerUtc)}).";
        var display = new List<ToolParamDisplay>
        {
            new("Határidő", deadline.Title),
            new("Emlékeztető", FormatLocal(triggerUtc)),
            new("Csatorna", DisplayChannel(channel)),
        };

        return new ToolResolution(true, resolved, summary, display, [], null);
    }

    private async Task<ToolResolution> ResolveWarrantyAsync(
        string anchorRef, int offsetDays, string channel, string? recurrence, ToolExecutionContext ctx, CancellationToken ct)
    {
        var candidates = await db.Warranties
            .AsNoTracking()
            .Where(w => w.WarrantyEndDate != null)
            .ToListAsync(ct);

        // Two plain queries + an in-memory join instead of .Include(w => w.Document): keeps
        // this testable with a simple DbSet fake (no EF-provider-specific Include support
        // needed) and avoids loading the Document navigation eagerly for warranties that
        // won't even match anchorRef.
        var documentIds = candidates.Select(w => w.DocumentId).Distinct().ToList();
        var documents = await db.Documents.AsNoTracking().Where(d => documentIds.Contains(d.Id)).ToListAsync(ct);
        var documentsById = documents.ToDictionary(d => d.Id);

        var visible = candidates
            .Where(w => documentsById.TryGetValue(w.DocumentId, out var doc) && authService.CanReadDocument(doc))
            .ToList();
        var (outcome, warranty) = RefMatcher.Match(visible, anchorRef, w => w.ProductName);

        if (outcome != RefMatchOutcome.Found || warranty is null)
            return NotFoundOrAmbiguous(outcome, "garanciát", anchorRef);

        // WarrantyEndDate has no time component — normalize to 09:00 in the user's TZ, then UTC
        // (ai-pipeline.md §11.2). Task/Deadline anchors already carry a full UTC instant, so no
        // normalization is needed there. The Where() clause above already guarantees non-null,
        // but EF's translated predicate isn't visible to C#'s nullable flow analysis, hence the
        // explicit local copy.
        var warrantyEndDate = warranty.WarrantyEndDate!.Value;
        var dueDateUtc = ToUtc0900(warrantyEndDate, ctx.TimeZoneId);
        var triggerUtc = dueDateUtc.AddDays(-offsetDays);
        var warrantyEndDateText = warrantyEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var resolved = BuildResolvedArgs(new
        {
            anchorType = "warranty",
            sourceDocumentId = warranty.DocumentId,
            productName = warranty.ProductName,
            warrantyEndDate = warrantyEndDateText,
            dueDateUtc,
            relatedFamilyMemberId = warranty.RelatedFamilyMemberId,
            triggerUtc,
            channel,
            recurrence,
            targetUserAccountId = ctx.UserAccountId,
            createdByUserId = ctx.UserAccountId,
        });

        var summary = $"Emlékeztetőt hozok létre a(z) \"{warranty.ProductName}\" garanciájának lejárta előtt {offsetDays} nappal ({FormatLocal(triggerUtc)}).";
        var display = new List<ToolParamDisplay>
        {
            new("Termék", warranty.ProductName),
            new("Lejárat", warrantyEndDateText),
            new("Emlékeztető", $"{FormatLocal(triggerUtc)} ({offsetDays} nappal előbb)"),
            new("Csatorna", DisplayChannel(channel)),
        };

        return new ToolResolution(true, resolved, summary, display, [], null);
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement resolvedArguments, ToolExecutionContext ctx, CancellationToken ct)
    {
        var anchorType = resolvedArguments.GetProperty("anchorType").GetString()!;
        var channel = Enum.Parse<NotificationChannel>(resolvedArguments.GetProperty("channel").GetString()!, ignoreCase: true);
        var recurrence = resolvedArguments.TryGetProperty("recurrence", out var recEl) && recEl.ValueKind == JsonValueKind.String
            ? recEl.GetString()
            : null;
        var triggerUtc = resolvedArguments.GetProperty("triggerUtc").GetDateTime();
        var targetUserAccountId = resolvedArguments.GetProperty("targetUserAccountId").GetGuid();
        var createdByUserId = resolvedArguments.GetProperty("createdByUserId").GetGuid();

        Guid? taskId = null;
        Guid? deadlineId = null;

        if (anchorType == "warranty")
        {
            var deadlineCmd = new CreateDeadlineCommand(
                Title: $"Garancia lejárat: {resolvedArguments.GetProperty("productName").GetString()}",
                Description: null,
                DueDateUtc: resolvedArguments.GetProperty("dueDateUtc").GetDateTime(),
                Category: DeadlineCategory.Other,
                RelatedFamilyMemberId: resolvedArguments.TryGetProperty("relatedFamilyMemberId", out var rfm) && rfm.ValueKind == JsonValueKind.String
                    ? rfm.GetGuid()
                    : null,
                IsPrivate: false,
                CreatedByUserAccountId: createdByUserId);

            var deadlineDto = await sender.Send(deadlineCmd, ct);
            deadlineId = deadlineDto.Id;
        }
        else if (anchorType == "task")
        {
            taskId = resolvedArguments.GetProperty("taskId").GetGuid();

            // Defense-in-depth backstop (code review finding on c43dd87): the proposal token
            // is valid for up to TOOLCALL_PROPOSAL_TTL_SECONDS (default 10 min, ADR-0011 D1),
            // during which the task could be deleted or its visibility could change — re-check
            // right before dispatching the write, don't just trust what ResolveAsync saw.
            var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, ct)
                ?? throw new NotFoundException("Task", taskId.Value);
            if (!authService.CanReadTask(task))
                throw new ForbiddenException("Nincs jogosultsága ehhez a feladathoz emlékeztetőt létrehozni.");
        }
        else
        {
            deadlineId = resolvedArguments.GetProperty("deadlineId").GetGuid();

            var deadline = await db.Deadlines.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deadlineId, ct)
                ?? throw new NotFoundException("Deadline", deadlineId.Value);
            if (!authService.CanReadDeadline(deadline))
                throw new ForbiddenException("Nincs jogosultsága ehhez a határidőhöz emlékeztetőt létrehozni.");
        }

        var reminderCmd = new CreateReminderCommand(
            TaskId: taskId,
            DeadlineId: deadlineId,
            TargetUserAccountId: targetUserAccountId,
            Channel: channel,
            TriggerUtc: triggerUtc,
            RruleExpression: recurrence,
            CreatedByUserId: createdByUserId);

        var reminderDto = await sender.Send(reminderCmd, ct);

        return new ToolResult("Reminder", reminderDto.Id, $"Emlékeztető létrehozva {FormatLocal(triggerUtc)}-ra.");
    }

    private static ToolResolution NotFoundOrAmbiguous(RefMatchOutcome outcome, string what, string anchorRef)
    {
        var error = outcome == RefMatchOutcome.Ambiguous
            ? $"Több {what} is illeszkedik erre: \"{anchorRef}\". Kérem, pontosítsa."
            : $"Nem található {what} ezzel a névvel: \"{anchorRef}\".";
        return ToolResolution.Failure(error);
    }

    private static DateTime ToUtc0900(DateOnly date, string timeZoneId)
    {
        var localNoon = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0, DateTimeKind.Unspecified);
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeToUtc(localNoon, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            // Defensive fallback for environments without IANA tzdata (e.g. minimal CI images) —
            // treat as UTC rather than fail the whole resolve step.
            return DateTime.SpecifyKind(localNoon, DateTimeKind.Utc);
        }
        catch (InvalidTimeZoneException)
        {
            return DateTime.SpecifyKind(localNoon, DateTimeKind.Utc);
        }
    }

    private static string FormatLocal(DateTime utc) => utc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static string DisplayChannel(string channel) => channel switch
    {
        "email" => "E-mail",
        _ => "Alkalmazáson belül",
    };

    private static JsonElement BuildResolvedArgs(object anonymous)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(anonymous));
        return doc.RootElement.Clone();
    }
}
