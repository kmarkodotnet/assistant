using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class Source
{
    private Source() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SourceKind Kind { get; private set; }
    public string ConfigJson { get; private set; } = "{}";
    public bool IsActive { get; private set; } = true;
    public DateTime? LastSyncUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    public static Source Create(string name, SourceKind kind, string configJson = "{}")
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Kind = kind,
            ConfigJson = configJson,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

    public void UpdateLastSync()
    {
        LastSyncUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void UpdateConfig(string configJson)
    {
        ConfigJson = configJson;
        UpdatedUtc = DateTime.UtcNow;
    }
}
