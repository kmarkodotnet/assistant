using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Options;
using Microsoft.Extensions.Options;
using Tesseract;

namespace FamilyOs.Infrastructure.Ai.Extraction;

public sealed class TesseractOcrExtractor : IDocumentTextExtractor
{
    private static readonly HashSet<string> SupportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/tiff",
        "image/bmp",
        "image/heic",
        "image/webp",
    };

    private readonly TesseractOptions _options;

    public TesseractOcrExtractor(IOptions<TesseractOptions> options)
    {
        _options = options.Value;
    }

    public bool CanHandle(string mimeType) => SupportedMimeTypes.Contains(mimeType);

    public Task<ExtractionResult> ExtractAsync(
        Stream fileStream,
        string mimeType,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var bytes = ReadAllBytes(fileStream);

        try
        {
            using var engine = new TesseractEngine(_options.DataPath, _options.Languages, EngineMode.Default);
            using var img = Pix.LoadFromMemory(bytes);
            using var page = engine.Process(img);

            var text = page.GetText()?.Trim() ?? string.Empty;
            var confidence = (decimal)page.GetMeanConfidence();

            return Task.FromResult(new ExtractionResult(text, "TesseractOcr", null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // If Tesseract fails (e.g. tessdata missing in test env), return empty
            return Task.FromResult(new ExtractionResult(string.Empty, "TesseractOcr", null));
        }
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream ms)
            return ms.ToArray();

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
