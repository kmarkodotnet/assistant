using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Storage;
using FamilyOs.Application.Common.Ai;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class ExtractTextJobRunner
{
    private readonly FamilyOsDbContext _db;
    private readonly IDocumentStorage _storage;
    private readonly IDocumentTextExtractor _extractor;
    private readonly IAiProcessingJobRepository _jobRepository;
    private readonly ILogger<ExtractTextJobRunner> _logger;

    // LoggerMessage delegates (CA1848 compliance)
    private static readonly Action<ILogger, Guid, Exception?> LogDocumentNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogDocumentNotFound)),
            "ExtractTextJobRunner: Document {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, Exception?> LogStorageOpenFailed =
        LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(2, nameof(LogStorageOpenFailed)),
            "ExtractTextJobRunner: failed to open storage for document {Id}.");

    private static readonly Action<ILogger, Guid, Exception?> LogNoTextExtracted =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(3, nameof(LogNoTextExtracted)),
            "ExtractTextJobRunner: no text extracted from document {Id}.");

    private static readonly Action<ILogger, int, Guid, ExtractionMethod, Exception?> LogExtractionDone =
        LoggerMessage.Define<int, Guid, ExtractionMethod>(LogLevel.Information, new EventId(4, nameof(LogExtractionDone)),
            "ExtractTextJobRunner: extracted {Chars} chars from document {Id} via {Method}.");

    public ExtractTextJobRunner(
        FamilyOsDbContext db,
        IDocumentStorage storage,
        IDocumentTextExtractor extractor,
        IAiProcessingJobRepository jobRepository,
        ILogger<ExtractTextJobRunner> logger)
    {
        _db = db;
        _storage = storage;
        _extractor = extractor;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task RunAsync(AiProcessingJob job, CancellationToken ct)
    {
        // 1. Load document with existing DocumentText
        var document = await _db.Documents
            .Include(d => d.DocumentText)
            .FirstOrDefaultAsync(d => d.Id == job.TargetId, ct);

        if (document is null)
        {
            LogDocumentNotFound(_logger, job.TargetId, null);
            return;
        }

        // 2. Set status to Extracting
        document.SetProcessingStatus(ProcessingStatus.Extracting);
        await _db.SaveChangesAsync(ct);

        // 3. Open file stream
        Stream fileStream;
        try
        {
            fileStream = await _storage.OpenReadAsync(document.StoragePath, ct);
        }
        catch (Exception ex)
        {
            LogStorageOpenFailed(_logger, document.Id, ex);
            document.SetProcessingStatus(ProcessingStatus.Failed);
            await _db.SaveChangesAsync(ct);
            throw;
        }

        // 4. Extract text
        ExtractionResult result;
        await using (fileStream)
        {
            result = await _extractor.ExtractAsync(fileStream, document.MimeType, ct);
        }

        // 5. Map string method name to enum value
        var method = MapExtractionMethod(result.ExtractionMethod);

        // 6. Guard: empty text means failure
        if (string.IsNullOrWhiteSpace(result.Text))
        {
            LogNoTextExtracted(_logger, document.Id, null);
            document.SetProcessingStatus(ProcessingStatus.Failed);
            await _db.SaveChangesAsync(ct);
            return;
        }

        // 7. Upsert DocumentText
        if (document.DocumentText is null)
        {
            var docText = DocumentText.Create(document.Id, result.Text, method, result.Language);
            await _db.DocumentTexts.AddAsync(docText, ct);
        }
        else
        {
            document.DocumentText.UpdateContent(result.Text, method, result.Language);
        }

        await _db.SaveChangesAsync(ct);

        // 8. Enqueue DetectLanguage job
        var detectJob = AiProcessingJob.Create(AiJobType.DetectLanguage, document.Id);
        await _jobRepository.AddAsync(detectJob, ct);
        await _jobRepository.SaveChangesAsync(ct);

        LogExtractionDone(_logger, result.Text.Length, document.Id, method, null);
    }

    private static ExtractionMethod MapExtractionMethod(string method) => method switch
    {
        "PdfTextLayer" => ExtractionMethod.PdfTextLayer,
        "TesseractOcr" => ExtractionMethod.TesseractOcr,
        "PlainText" => ExtractionMethod.PlainText,
        "DocxExtract" => ExtractionMethod.DocxExtract,
        "ManualPaste" => ExtractionMethod.ManualPaste,
        "EmailBody" => ExtractionMethod.EmailBody,
        _ => ExtractionMethod.PdfTextLayer,
    };
}
