using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class Document
{
    private Document() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public string Sha256 { get; private set; } = string.Empty;
    public SourceType SourceType { get; private set; }
    public Guid? SourceEmailMessageId { get; private set; }
    public string? Language { get; private set; }
    public DateOnly? DocumentDate { get; private set; }
    public Guid? RelatedFamilyMemberId { get; private set; }
    public bool IsPrivate { get; private set; }
    public ProcessingStatus ProcessingStatus { get; private set; }
    public Origin Origin { get; private set; }
    public Guid CreatedByUserAccountId { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    // Navigations
    public DocumentText? DocumentText { get; private set; }
    public FamilyMember? RelatedFamilyMember { get; private set; }

    public static Document Create(
        string title,
        string originalFileName,
        string mimeType,
        long sizeBytes,
        string storagePath,
        string sha256,
        SourceType sourceType,
        Origin origin,
        Guid createdByUserAccountId,
        DateOnly? documentDate = null,
        Guid? relatedFamilyMemberId = null,
        bool isPrivate = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            OriginalFileName = originalFileName,
            MimeType = mimeType,
            SizeBytes = sizeBytes,
            StoragePath = storagePath,
            Sha256 = sha256,
            SourceType = sourceType,
            Origin = origin,
            CreatedByUserAccountId = createdByUserAccountId,
            DocumentDate = documentDate,
            RelatedFamilyMemberId = relatedFamilyMemberId,
            IsPrivate = isPrivate,
            ProcessingStatus = ProcessingStatus.Pending,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

    public void UpdateMetadata(
        string? title,
        DateOnly? documentDate,
        Guid? relatedFamilyMemberId,
        bool? isPrivate)
    {
        if (title is not null) Title = title;
        if (documentDate.HasValue) DocumentDate = documentDate;
        if (relatedFamilyMemberId.HasValue) RelatedFamilyMemberId = relatedFamilyMemberId;
        if (isPrivate.HasValue) IsPrivate = isPrivate.Value;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void SetProcessingStatus(ProcessingStatus status)
    {
        ProcessingStatus = status;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }
}
