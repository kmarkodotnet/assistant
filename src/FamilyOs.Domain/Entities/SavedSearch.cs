namespace FamilyOs.Domain.Entities;

public sealed class SavedSearch
{
    private SavedSearch() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string QueryJson { get; private set; } = string.Empty;
    public Guid UserAccountId { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    public static SavedSearch Create(string name, string queryJson, Guid userAccountId)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            QueryJson = queryJson,
            UserAccountId = userAccountId,
            CreatedUtc = DateTime.UtcNow,
        };
}
