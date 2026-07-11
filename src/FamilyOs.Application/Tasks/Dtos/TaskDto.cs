using FamilyOs.Domain.Enums;

namespace FamilyOs.Application.Tasks.Dtos;

public sealed class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public Guid? SourceDocumentId { get; set; }
    public Guid? AssignedToFamilyMemberId { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public Guid? ApprovedByUserAccountId { get; set; }
    public DateTime? ApprovedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
}

public sealed class TaskListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public Guid? AssignedToFamilyMemberId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string? SourceDocumentTitle { get; set; }
    public string? CardSummary { get; set; }
}
