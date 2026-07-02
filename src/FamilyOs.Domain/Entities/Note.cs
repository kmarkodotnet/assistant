namespace FamilyOs.Domain.Entities;

public sealed class Note
{
    private Note() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public Guid? RelatedFamilyMemberId { get; private set; }
    public Guid CreatedByUserAccountId { get; private set; }
    public bool IsPrivate { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    // Navigations
    public FamilyMember? RelatedFamilyMember { get; private set; }
    public ICollection<NoteChunk> Chunks { get; private set; } = [];
    public ICollection<NoteTag> NoteTags { get; private set; } = [];
    public ICollection<NoteTopic> NoteTopics { get; private set; } = [];

    public static Note Create(
        string title,
        string body,
        Guid createdByUserId,
        Guid? relatedFamilyMemberId = null,
        bool isPrivate = false)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            Body = body,
            CreatedByUserAccountId = createdByUserId,
            RelatedFamilyMemberId = relatedFamilyMemberId,
            IsPrivate = isPrivate,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

    public void UpdateContent(string title, string body)
    {
        Title = title;
        Body = body;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }
}
