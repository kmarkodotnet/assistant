namespace FamilyOs.Application.Abstractions.Ai;

public record AnswerResult(string Answer, string[] CitedSourceIds, double Confidence);

public interface IQuestionAnswerService
{
    Task<AnswerResult> AnswerAsync(
        string question,
        IReadOnlyList<(string chunkId, string content)> sources,
        CancellationToken ct = default);
}
