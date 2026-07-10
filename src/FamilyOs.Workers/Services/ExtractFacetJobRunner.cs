using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class ExtractFacetJobRunner
{
    private readonly FamilyOsDbContext _db;
    private readonly IDocumentClassifier _classifier;
    private readonly IWarrantyExtractor _warrantyExtractor;
    private readonly IMedicalRecordExtractor _medicalExtractor;
    private readonly IFinancialRecordExtractor _financialExtractor;
    private readonly IProcessingProgressNotifier _notifier;
    private readonly ILogger<ExtractFacetJobRunner> _logger;

    private static readonly Action<ILogger, Guid, Exception?> LogDocumentNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogDocumentNotFound)),
            "ExtractFacetJobRunner: Document {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, string?, Exception?> LogFacetProcessed =
        LoggerMessage.Define<Guid, string?>(LogLevel.Information, new EventId(2, nameof(LogFacetProcessed)),
            "ExtractFacetJobRunner: document {Id} facet processed as '{FacetType}'.");

    public ExtractFacetJobRunner(
        FamilyOsDbContext db,
        IDocumentClassifier classifier,
        IWarrantyExtractor warrantyExtractor,
        IMedicalRecordExtractor medicalExtractor,
        IFinancialRecordExtractor financialExtractor,
        IProcessingProgressNotifier notifier,
        ILogger<ExtractFacetJobRunner> logger)
    {
        _db = db;
        _classifier = classifier;
        _warrantyExtractor = warrantyExtractor;
        _medicalExtractor = medicalExtractor;
        _financialExtractor = financialExtractor;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task RunAsync(AiProcessingJob job, CancellationToken ct)
    {
        var document = await _db.Documents
            .Include(d => d.DocumentText)
            .FirstOrDefaultAsync(d => d.Id == job.TargetId, ct);

        if (document is null || document.DocumentText is null)
        {
            LogDocumentNotFound(_logger, job.TargetId, null);
            return;
        }

        await _notifier.NotifyProgressAsync(job.TargetId, "ExtractFacet", 0, ct);

        var text = document.DocumentText.Content;

        // Classify to determine facet type
        var classification = await _classifier.ClassifyAsync(text, ct);
        var facetType = classification.FacetType;

        switch (facetType)
        {
            case "Warranty":
                await ProcessWarrantyAsync(document, text, ct);
                break;

            case "Medical":
                await ProcessMedicalAsync(document, text, ct);
                break;

            case "Financial":
                await ProcessFinancialAsync(document, text, ct);
                break;

            default:
                // Unknown or null facet type — nothing to do
                break;
        }

        await _notifier.NotifyProgressAsync(job.TargetId, "ExtractFacet", 100, ct);

        LogFacetProcessed(_logger, job.TargetId, facetType, null);
    }

    private async System.Threading.Tasks.Task ProcessWarrantyAsync(Document document, string text, CancellationToken ct)
    {
        var extraction = await _warrantyExtractor.ExtractAsync(text, ct);
        if (extraction is null) return;

        var existing = await _db.Warranties
            .FirstOrDefaultAsync(w => w.DocumentId == document.Id, ct);

        if (existing is null)
        {
            var warranty = Warranty.Create(document.Id, extraction.ProductName ?? "Unknown Product");
            warranty.Patch(
                extraction.ProductName,
                null, null, null,
                extraction.PurchaseDate,
                null, null,
                extraction.WarrantyMonths,
                extraction.ExpiryDate,
                null, null);
            await _db.Warranties.AddAsync(warranty, ct);
        }
        else
        {
            existing.Patch(
                extraction.ProductName,
                null, null, null,
                extraction.PurchaseDate,
                null, null,
                extraction.WarrantyMonths,
                extraction.ExpiryDate,
                null, null);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async System.Threading.Tasks.Task ProcessMedicalAsync(Document document, string text, CancellationToken ct)
    {
        var extraction = await _medicalExtractor.ExtractAsync(text, ct);
        if (extraction is null) return;

        // RelatedFamilyMemberId is required for medical records — use Document's related member
        if (!document.RelatedFamilyMemberId.HasValue)
        {
            // Cannot resolve family member — skip as per spec
            return;
        }

        var recordDate = extraction.RecordDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var recordType = ParseMedicalRecordType(extraction.RecordType);

        var existing = await _db.MedicalRecords
            .FirstOrDefaultAsync(r => r.DocumentId == document.Id, ct);

        if (existing is null)
        {
            var title = extraction.Diagnosis ?? extraction.RecordType ?? "Medical Record";
            var record = MedicalRecord.Create(
                document.Id,
                document.RelatedFamilyMemberId.Value,
                recordType,
                recordDate,
                title);
            await _db.MedicalRecords.AddAsync(record, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async System.Threading.Tasks.Task ProcessFinancialAsync(Document document, string text, CancellationToken ct)
    {
        var extraction = await _financialExtractor.ExtractAsync(text, ct);
        if (extraction is null) return;

        var recordType = ParseFinancialRecordType(extraction.RecordType);
        var recurrencePeriod = ParseRecurrencePeriod(extraction.RecurrencePeriod);

        var existing = await _db.FinancialRecords
            .FirstOrDefaultAsync(r => r.DocumentId == document.Id, ct);

        if (existing is null)
        {
            var record = FinancialRecord.Create(document.Id, recordType);
            record.Patch(
                recordType,
                extraction.Vendor,
                extraction.Amount,
                extraction.Currency,
                extraction.IssueDate,
                extraction.DueDate,
                extraction.IsPaid,
                recurrencePeriod,
                document.RelatedFamilyMemberId);
            await _db.FinancialRecords.AddAsync(record, ct);
        }
        else
        {
            existing.Patch(
                recordType,
                extraction.Vendor,
                extraction.Amount,
                extraction.Currency,
                extraction.IssueDate,
                extraction.DueDate,
                extraction.IsPaid,
                recurrencePeriod,
                document.RelatedFamilyMemberId);
        }

        await _db.SaveChangesAsync(ct);
    }

    private static FinancialRecordType ParseFinancialRecordType(string? recordType)
    {
        if (string.IsNullOrWhiteSpace(recordType))
            return FinancialRecordType.Other;

        return recordType.ToLowerInvariant() switch
        {
            "invoice" or "szamla" => FinancialRecordType.Invoice,
            "receipt" or "nyugta" => FinancialRecordType.Receipt,
            "insurance" or "biztositas" => FinancialRecordType.Insurance,
            "subscription" or "elofizetes" => FinancialRecordType.Subscription,
            "bankstatement" or "bankszamlakivonat" => FinancialRecordType.BankStatement,
            "contract" or "szerzodes" => FinancialRecordType.Contract,
            _ => FinancialRecordType.Other,
        };
    }

    private static RecurrencePeriod? ParseRecurrencePeriod(string? recurrencePeriod)
    {
        if (string.IsNullOrWhiteSpace(recurrencePeriod))
            return null;

        return recurrencePeriod.ToLowerInvariant() switch
        {
            "monthly" => RecurrencePeriod.Monthly,
            "quarterly" => RecurrencePeriod.Quarterly,
            "yearly" => RecurrencePeriod.Yearly,
            "none" => RecurrencePeriod.None,
            _ => null,
        };
    }

    private static MedicalRecordType ParseMedicalRecordType(string? recordType)
    {
        if (string.IsNullOrWhiteSpace(recordType))
            return MedicalRecordType.Other;

        return recordType.ToLowerInvariant() switch
        {
            "prescription" or "rendelvény" => MedicalRecordType.Prescription,
            "labresult" or "lab" or "lelet" => MedicalRecordType.LabResult,
            "imaging" or "röntgen" or "ct" or "mri" => MedicalRecordType.Imaging,
            "vaccination" or "oltás" => MedicalRecordType.Vaccination,
            "diagnosis" or "diagnózis" => MedicalRecordType.Diagnosis,
            "appointmentnote" or "appointment" or "beutaló" or "zárójelentés" => MedicalRecordType.AppointmentNote,
            _ => MedicalRecordType.Other,
        };
    }
}
