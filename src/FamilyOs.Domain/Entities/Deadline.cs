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
}
