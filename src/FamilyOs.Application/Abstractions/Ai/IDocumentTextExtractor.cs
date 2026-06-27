namespace FamilyOs.Application.Abstractions.Ai;

public record ExtractionResult(string Text, string ExtractionMethod, string? Language);

public interface IDocumentTextExtractor
{
    bool CanHandle(string mimeType);
    Task<ExtractionResult> ExtractAsync(Stream fileStream, string mimeType, CancellationToken ct = default);
}
