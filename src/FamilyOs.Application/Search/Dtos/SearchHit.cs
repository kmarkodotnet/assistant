namespace FamilyOs.Application.Search.Dtos;

public sealed class SearchHit
{
    public string EntityType { get; set; } = string.Empty; // "document", "task", "deadline"
    public Guid EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public double Score { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}
