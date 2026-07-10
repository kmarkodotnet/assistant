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
            Id = Guid.CreateVersion7(),
            DocumentId = documentId,
            RecordType = recordType,
            RecurrencePeriod = RecurrencePeriod.None,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

    public void Patch(
        FinancialRecordType? recordType,
        string? vendor,
        decimal? amount,
        string? currency,
        DateOnly? issueDate,
        DateOnly? dueDate,
        bool? isPaid,
        RecurrencePeriod? recurrencePeriod,
        Guid? relatedFamilyMemberId)
    {
        if (recordType.HasValue) RecordType = recordType.Value;
        Vendor = vendor ?? Vendor;
        Amount = amount ?? Amount;
        Currency = currency ?? Currency;
        IssueDate = issueDate ?? IssueDate;
        DueDate = dueDate ?? DueDate;
        if (recurrencePeriod.HasValue) RecurrencePeriod = recurrencePeriod.Value;
        RelatedFamilyMemberId = relatedFamilyMemberId ?? RelatedFamilyMemberId;

        // ck_financial_paid requires paid_date whenever is_paid = true. AI extraction has no
        // explicit paid date signal (e.g. a receipt implies "paid on the issue date"), so we
        // only flip IsPaid on when a usable date exists — never violate the DB constraint.
        if (isPaid == true)
        {
            var effectivePaidDate = PaidDate ?? issueDate ?? IssueDate;
            if (effectivePaidDate is not null)
            {
                IsPaid = true;
                PaidDate = effectivePaidDate;
            }
        }
        else if (isPaid == false)
        {
            IsPaid = false;
        }

        UpdatedUtc = DateTime.UtcNow;
    }
}
