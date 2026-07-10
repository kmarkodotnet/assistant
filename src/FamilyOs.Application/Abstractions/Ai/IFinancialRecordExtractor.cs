namespace FamilyOs.Application.Abstractions.Ai;

public record FinancialRecordExtraction(
    string? RecordType,
    string? Vendor,
    decimal? Amount,
    string? Currency,
    DateOnly? IssueDate,
    DateOnly? DueDate,
    bool? IsPaid,
    string? RecurrencePeriod);

public interface IFinancialRecordExtractor
{
    Task<FinancialRecordExtraction?> ExtractAsync(string text, CancellationToken ct = default);
}
