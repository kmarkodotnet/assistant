namespace FamilyOs.Domain.Entities;

public sealed class Topic
{
    private Topic() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public string? Icon { get; private set; }
    public int SortOrder { get; private set; }
    public Topic? Parent { get; private set; }
    public ICollection<Topic> Children { get; private set; } = [];
    public ICollection<DocumentTopic> DocumentTopics { get; private set; } = [];
    public ICollection<NoteTopic> NoteTopics { get; private set; } = [];
    public DateTime CreatedUtc { get; private set; }

    public static Topic Create(string name, string slug, Guid? parentId = null, string? icon = null, int sortOrder = 0)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            ParentId = parentId,
            Icon = icon,
            SortOrder = sortOrder,
            CreatedUtc = DateTime.UtcNow,
        };

    public void Update(string? name, string? icon, int? sortOrder)
    {
        if (name is not null) Name = name;
        if (icon is not null) Icon = icon;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
    }
}
