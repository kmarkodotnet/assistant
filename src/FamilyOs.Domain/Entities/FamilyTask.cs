using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

// Named FamilyTask to avoid collision with System.Threading.Tasks.Task
public sealed class FamilyTask
{
    private FamilyTask() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    public Enums.TaskStatus Status { get; private set; }
    public Priority Priority { get; private set; }
    public Origin Origin { get; private set; }
    public Guid? SourceDocumentId { get; private set; }
    public Guid? AssignedToFamilyMemberId { get; private set; }
    public Guid CreatedByUserAccountId { get; private set; }
    public bool IsPrivate { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }
    public Document? SourceDocument { get; private set; }

    public static FamilyTask CreateSuggestion(
        string title,
        Guid sourceDocumentId,
        Guid createdByUserAccountId,
        DateTime? dueDateUtc = null,
        string? description = null,
        Guid? assignedToFamilyMemberId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            DueDateUtc = dueDateUtc,
            Status = Enums.TaskStatus.Suggested,
            Priority = Priority.Normal,
            Origin = Origin.AiSuggested,
            SourceDocumentId = sourceDocumentId,
            AssignedToFamilyMemberId = assignedToFamilyMemberId,
            CreatedByUserAccountId = createdByUserAccountId,
            IsPrivate = false,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };
}
