using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using FamilyOs.Application.Abstractions.Ai;

namespace FamilyOs.Infrastructure.Ai.Extraction;

public sealed class CompositeDocumentTextExtractor : IDocumentTextExtractor
{
    private readonly PdfTextLayerExtractor _pdfExtractor;
    private readonly TesseractOcrExtractor _ocrExtractor;

    public CompositeDocumentTextExtractor(
        PdfTextLayerExtractor pdfExtractor,
        TesseractOcrExtractor ocrExtractor)
    {
        _pdfExtractor = pdfExtractor;
        _ocrExtractor = ocrExtractor;
    }

    public bool CanHandle(string mimeType) =>
        _pdfExtractor.CanHandle(mimeType)
        || _ocrExtractor.CanHandle(mimeType)
        || IsPlainText(mimeType)
        || IsDocx(mimeType);

    public async Task<ExtractionResult> ExtractAsync(
        Stream fileStream,
        string mimeType,
        CancellationToken ct = default)
    {
        if (IsPlainText(mimeType))
        {
            return await ExtractPlainTextAsync(fileStream, ct);
        }

        if (IsDocx(mimeType))
        {
            return await ExtractDocxAsync(fileStream, ct);
        }

        if (string.Equals(mimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return await ExtractPdfAsync(fileStream, mimeType, ct);
        }

        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return await _ocrExtractor.ExtractAsync(fileStream, mimeType, ct);
        }

        return new ExtractionResult(string.Empty, "Unsupported", null);
    }

    private async Task<ExtractionResult> ExtractPdfAsync(
        Stream fileStream,
        string mimeType,
        CancellationToken ct)
    {
        // Buffer the stream so it can be re-read
        var bytes = await ReadAllBytesAsync(fileStream, ct);

        // Try PDF text layer first; fall back to OCR on any PDF parse error
        ExtractionResult? result = null;
        try
        {
            using var firstStream = new MemoryStream(bytes);
            result = await _pdfExtractor.ExtractAsync(firstStream, mimeType, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // PdfPig throws on corrupt/invalid PDFs — fall through to OCR
        }

        if (result is not null)
        {
            var printableRatio = CountPrintable(result.Text) / (double)Math.Max(result.Text.Length, 1);

            if (result.Text.Length >= 100 && printableRatio >= 0.8)
            {
                return result;
            }
        }

        // Fallback to OCR
        using var secondStream = new MemoryStream(bytes);
        return await _ocrExtractor.ExtractAsync(secondStream, mimeType, ct);
    }

    private static async Task<ExtractionResult> ExtractPlainTextAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = await reader.ReadToEndAsync(ct);
        return new ExtractionResult(text.Trim(), "PlainText", null);
    }

    private static Task<ExtractionResult> ExtractDocxAsync(Stream stream, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var documentXml = zip.GetEntry("word/document.xml");
            if (documentXml is null)
                return Task.FromResult(new ExtractionResult(string.Empty, "DocxExtract", null));

            using var xmlStream = documentXml.Open();
            using var reader = new StreamReader(xmlStream, Encoding.UTF8);
            var xml = reader.ReadToEnd();

            // Strip XML tags
            var text = Regex.Replace(xml, "<[^>]+>", " ");
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return Task.FromResult(new ExtractionResult(text, "DocxExtract", null));
        }
        catch
        {
            return Task.FromResult(new ExtractionResult(string.Empty, "DocxExtract", null));
        }
    }

    private static int CountPrintable(string text)
        => text.Count(c => !char.IsControl(c));

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken ct)
    {
        if (stream is MemoryStream ms)
            return ms.ToArray();

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }

    private static bool IsPlainText(string mimeType) =>
        string.Equals(mimeType, "text/plain", StringComparison.OrdinalIgnoreCase);

    private static bool IsDocx(string mimeType) =>
        string.Equals(mimeType,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            StringComparison.OrdinalIgnoreCase);
}
