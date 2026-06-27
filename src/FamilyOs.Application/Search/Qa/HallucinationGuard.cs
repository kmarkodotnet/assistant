using FamilyOs.Application.Abstractions.Ai;

namespace FamilyOs.Application.Search.Qa;

public static class HallucinationGuard
{
    private const string FallbackAnswer =
        "Nincs erre vonatkozó adat a rendelkezésre álló dokumentumokban.";

    public static AnswerResult Validate(
        AnswerResult answer,
        IReadOnlyList<string> retrievedChunkIds)
    {
        var invalid = answer.CitedSourceIds.Except(retrievedChunkIds).Any();
        if (invalid)
            return new AnswerResult(FallbackAnswer, [], 0.0);
        return answer;
    }
}
