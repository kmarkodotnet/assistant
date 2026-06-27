using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Search.Dtos;
using FamilyOs.Application.Search.Qa;

namespace FamilyOs.Application.Search.Handlers;

public sealed class QaHandler
{
    private readonly HybridSearchHandler _hybridHandler;
    private readonly IQuestionAnswerService _qaService;

    public QaHandler(HybridSearchHandler hybridHandler, IQuestionAnswerService qaService)
    {
        _hybridHandler = hybridHandler;
        _qaService = qaService;
    }

    public async Task<SearchResponse> SearchAsync(
        SearchRequest req,
        Guid? userId,
        CancellationToken ct)
    {
        var (searchResponse, topChunks) = await _hybridHandler.SearchWithChunksAsync(req, userId, ct);

        if (topChunks.Count == 0)
        {
            return new SearchResponse
            {
                Hits = searchResponse.Hits,
                TotalCount = searchResponse.TotalCount,
                ModeUsed = SearchMode.Qa,
                Answer = "Nincs erre vonatkozó adat a rendelkezésre álló dokumentumokban.",
                Confidence = 0.0,
            };
        }

        var answer = await _qaService.AnswerAsync(req.Query, topChunks, ct);
        var retrievedIds = topChunks.Select(c => c.chunkId).ToList();
        var validated = HallucinationGuard.Validate(answer, retrievedIds);

        return new SearchResponse
        {
            Hits = searchResponse.Hits,
            TotalCount = searchResponse.TotalCount,
            ModeUsed = SearchMode.Qa,
            Answer = validated.Answer,
            AnswerSources = validated.CitedSourceIds,
            Confidence = validated.Confidence,
        };
    }
}
