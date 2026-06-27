namespace FamilyOs.Application.Abstractions.Ai;

public record ClassificationResult(string[] Topics, string[] Tags, string? FacetType);

public interface IDocumentClassifier
{
    Task<ClassificationResult> ClassifyAsync(string documentText, CancellationToken ct = default);
}
