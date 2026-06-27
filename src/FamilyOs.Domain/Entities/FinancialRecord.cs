using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class FinancialRecord
{
    private FinancialRecord() { }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public FinancialRecordType RecordType { get; private set; }
    public string? Vendor { get; private set; }
    public decimal? Amount { get; private set; }
    public string? Currency { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateOnly? PaidDate { get; private set; }
    public bool IsPaid { get; private set; }
    public RecurrencePeriod RecurrencePeriod { get; private set; }
    public Guid? RelatedFamilyMemberId { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    public Document? Document { get; private set; }

    public static FinancialRecord Create(Guid documentId, FinancialRecordType recordType)
        => new()
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            RecordType = recordType,
            RecurrencePeriod = RecurrencePeriod.None,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };
}
