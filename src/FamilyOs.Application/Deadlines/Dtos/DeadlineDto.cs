namespace FamilyOs.Application.Deadlines.Dtos;

public sealed class DeadlineDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public Guid? SourceDocumentId { get; set; }
    public Guid? RelatedFamilyMemberId { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public Guid? ApprovedByUserAccountId { get; set; }
    public DateTime? ApprovedUtc { get; set; }
}

public sealed class DeadlineListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DueDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public Guid? RelatedFamilyMemberId { get; set; }
    public DateTime CreatedUtc { get; set; }
}
