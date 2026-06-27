namespace FamilyOs.Domain.Entities;

public sealed class Topic
{
    private Topic() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public Topic? Parent { get; private set; }
    public ICollection<Topic> Children { get; private set; } = [];
    public ICollection<DocumentTopic> DocumentTopics { get; private set; } = [];
    public DateTime CreatedUtc { get; private set; }

    public static Topic Create(string name, string slug, Guid? parentId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            ParentId = parentId,
            CreatedUtc = DateTime.UtcNow,
        };
}
