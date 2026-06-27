using FamilyOs.Application.Abstractions.Ai;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FamilyOs.Infrastructure.Ai.Extraction;

public sealed class PdfTextLayerExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string mimeType) =>
        string.Equals(mimeType, "application/pdf", StringComparison.OrdinalIgnoreCase);

    public Task<ExtractionResult> ExtractAsync(
        Stream fileStream,
        string mimeType,
        CancellationToken ct = default)
    {
        var bytes = ReadAllBytes(fileStream);
        var textBuilder = new System.Text.StringBuilder();

        using var pdfDoc = PdfDocument.Open(bytes);
        foreach (var page in pdfDoc.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            var pageText = string.Join(" ", page.GetWords().Select(w => w.Text));
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                textBuilder.AppendLine(pageText);
            }
        }

        var text = textBuilder.ToString().Trim();

        return Task.FromResult(new ExtractionResult(text, "PdfTextLayer", null));
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
