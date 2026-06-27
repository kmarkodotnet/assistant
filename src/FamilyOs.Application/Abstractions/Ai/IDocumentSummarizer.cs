namespace FamilyOs.Application.Abstractions.Ai;

public record SummaryResult(string Summary, string ModelName, string PromptVersion);

public interface IDocumentSummarizer
{
    Task<SummaryResult> SummarizeAsync(string documentText, string language, CancellationToken ct = default);
}
