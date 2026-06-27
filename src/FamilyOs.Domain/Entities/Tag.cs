namespace FamilyOs.Domain.Entities;

public sealed class Tag
{
    private Tag() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int UsageCount { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public ICollection<DocumentTag> DocumentTags { get; private set; } = [];

    public static Tag Create(string name)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name.ToLowerInvariant().Trim(),
            UsageCount = 1,
            CreatedUtc = DateTime.UtcNow,
        };

    public void IncrementUsage() { UsageCount++; }
}
