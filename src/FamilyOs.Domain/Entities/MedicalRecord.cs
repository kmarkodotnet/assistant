using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class MedicalRecord
{
    private MedicalRecord() { }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid FamilyMemberId { get; private set; }
    public MedicalRecordType RecordType { get; private set; }
    public DateOnly RecordDate { get; private set; }
    public string? Provider { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? StructuredJson { get; private set; }
    public bool IsPrivate { get; private set; } = true;
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    public Document? Document { get; private set; }
    public FamilyMember? FamilyMember { get; private set; }

    public static MedicalRecord Create(
        Guid documentId,
        Guid familyMemberId,
        MedicalRecordType recordType,
        DateOnly recordDate,
        string title)
        => new()
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            FamilyMemberId = familyMemberId,
            RecordType = recordType,
            RecordDate = recordDate,
            Title = title,
            IsPrivate = true,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };
}
