namespace FamilyOs.Application.Abstractions.Ai;

public interface IEmbedder
{
    string ModelName { get; }
    int Dimensions { get; }
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default);
}
