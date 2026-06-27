namespace FamilyOs.Application.Notes.Dtos;

public sealed class NoteDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? RelatedFamilyMemberId { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class NoteListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? RelatedFamilyMemberId { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
