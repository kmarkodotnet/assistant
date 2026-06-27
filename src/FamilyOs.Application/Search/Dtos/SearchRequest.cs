namespace FamilyOs.Application.Search.Dtos;

public enum SearchMode { Auto, Filter, Text, Semantic, Qa }

public sealed class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public SearchMode Mode { get; set; } = SearchMode.Auto;
    public string[]? EntityTypes { get; set; } // "documents", "tasks", "deadlines"
    public string[]? TopicSlugs { get; set; }
    public string[]? TagNames { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? RelatedFamilyMemberId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
