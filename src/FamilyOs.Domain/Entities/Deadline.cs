using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class Deadline
{
    private Deadline() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public DeadlineStatus Status { get; private set; }
    public DeadlineCategory Category { get; private set; }
    public Origin Origin { get; private set; }
    public Guid? SourceDocumentId { get; private set; }
    public Guid? RelatedFamilyMemberId { get; private set; }
    public Guid CreatedByUserAccountId { get; private set; }
    public bool IsPrivate { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }
    public Guid? ApprovedByUserAccountId { get; private set; }
    public DateTime? ApprovedUtc { get; private set; }

    // Navigations
    public Document? SourceDocument { get; private set; }

    public static Deadline CreateSuggestion(
        string title,
        DateTime dueDateUtc,
        Guid sourceDocumentId,
        Guid createdByUserAccountId,
        string? description = null,
        DeadlineCategory category = DeadlineCategory.Other)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            DueDateUtc = dueDateUtc,
            Status = DeadlineStatus.Upcoming,
            Category = category,
            Origin = Origin.AiSuggested,
            SourceDocumentId = sourceDocumentId,
            CreatedByUserAccountId = createdByUserAccountId,
            IsPrivate = false,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

    public static Deadline Create(
        string title,
        DateTime dueDateUtc,
        Guid createdByUserAccountId,
        string? description = null,
        DeadlineCategory category = DeadlineCategory.Other,
        Guid? relatedFamilyMemberId = null,
        bool isPrivate = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            DueDateUtc = dueDateUtc,
            Status = DeadlineStatus.Upcoming,
            Category = category,
            Origin = Origin.Manual,
            RelatedFamilyMemberId = relatedFamilyMemberId,
            CreatedByUserAccountId = createdByUserAccountId,
            IsPrivate = isPrivate,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

    public void SetStatus(DeadlineStatus status)
    {
        Status = status;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Approve(Guid approvedByUserId)
    {
        ApprovedByUserAccountId = approvedByUserId;
        ApprovedUtc = DateTime.UtcNow;
        Origin = Origin.AiApproved;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Resolve()
    {
        Status = DeadlineStatus.Resolved;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Dismiss()
    {
        Status = DeadlineStatus.Dismissed;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string? title,
        string? description,
        DateTime? dueDateUtc,
        DeadlineCategory? category,
        Guid? relatedFamilyMemberId,
        bool? isPrivate)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (dueDateUtc.HasValue) DueDateUtc = dueDateUtc.Value;
        if (category.HasValue) Category = category.Value;
        if (relatedFamilyMemberId.HasValue) RelatedFamilyMemberId = relatedFamilyMemberId;
        if (isPrivate.HasValue) IsPrivate = isPrivate.Value;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }
}
