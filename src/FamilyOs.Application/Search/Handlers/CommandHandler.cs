using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Ai;
using FamilyOs.Application.Search.Dtos;

namespace FamilyOs.Application.Search.Handlers;

/// <summary>
/// SearchMode.Command (api-design.md §16.1). Delegates to ToolCallPlanner; falls back to the
/// normal Q&amp;A flow when the LLM says "none" (ai-pipeline.md §11.3 step 2).
/// </summary>
public sealed class CommandHandler(ToolCallPlanner planner, QaHandler qaHandler)
{
    // No per-user TimeZoneId exists anywhere in the domain model yet (grep confirms it) — the
    // whole product is single-locale (hu-HU) for MVP, so this is a fixed constant rather than
    // a half-built per-user preference.
    private const string DefaultTimeZoneId = "Europe/Budapest";

    public async Task<SearchResponse> SearchAsync(
        SearchRequest req, Guid? userId, Guid? familyMemberId, string? role, CancellationToken ct)
    {
        if (userId is null)
        {
            return new SearchResponse
            {
                ModeUsed = SearchMode.Command,
                Answer = "A parancs végrehajtásához bejelentkezés szükséges.",
            };
        }

        var ctx = new ToolExecutionContext(userId.Value, familyMemberId, role ?? "Adult", DateTime.UtcNow, DefaultTimeZoneId);
        var plan = await planner.PlanAsync(req.Query, ctx, ct);

        switch (plan.Outcome)
        {
            case ToolPlanOutcome.FallbackToQa:
                var qaResponse = await qaHandler.SearchAsync(req, userId, ct);
                qaResponse.ModeUsed = SearchMode.Command;
                return qaResponse;

            case ToolPlanOutcome.Ready:
                return new SearchResponse { ModeUsed = SearchMode.Command, ToolCallProposal = plan.Proposal };

            default: // ParseFailed / ResolveFailed
                return new SearchResponse { ModeUsed = SearchMode.Command, Answer = plan.Message };
        }
    }
}
