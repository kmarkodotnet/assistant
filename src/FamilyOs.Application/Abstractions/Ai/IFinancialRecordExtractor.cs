namespace FamilyOs.Application.Abstractions.Ai;

public record FinancialRecordExtraction(decimal? Amount, string? Currency, DateOnly? RecordDate, bool IsPaid, string? RecurrencePeriod, string? Notes);

public interface IFinancialRecordExtractor
{
    Task<FinancialRecordExtraction?> ExtractAsync(string text, CancellationToken ct = default);
}
