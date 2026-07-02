namespace FamilyOs.Domain.Entities;

public sealed class Tag
{
    private Tag() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Color { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public ICollection<DocumentTag> DocumentTags { get; private set; } = [];
    public ICollection<NoteTag> NoteTags { get; private set; } = [];

    public static Tag Create(string name, string? color = null)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Name = name.ToLowerInvariant().Trim(),
            Color = color,
            UsageCount = 1,
            CreatedUtc = DateTime.UtcNow,
        };

    public void Update(string? name, string? color)
    {
        if (name is not null) Name = name.ToLowerInvariant().Trim();
        if (color is not null) Color = color;
    }

    public void IncrementUsage() { UsageCount++; }
    public void DecrementUsage() { if (UsageCount > 0) UsageCount--; }
}
