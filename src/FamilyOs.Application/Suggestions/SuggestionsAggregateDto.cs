using FamilyOs.Application.Deadlines.Dtos;
using FamilyOs.Application.Tasks.Dtos;

namespace FamilyOs.Application.Suggestions;

public sealed class DocumentTagSuggestionDto
{
    public Guid DocumentId { get; set; }
    public Guid TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
}

public sealed class DocumentTopicSuggestionDto
{
    public Guid DocumentId { get; set; }
    public Guid TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string TopicSlug { get; set; } = string.Empty;
}

public sealed class SuggestionsAggregateDto
{
    public IReadOnlyList<TaskListItemDto> Tasks { get; set; } = [];
    public IReadOnlyList<DeadlineListItemDto> Deadlines { get; set; } = [];
    public IReadOnlyList<DocumentTagSuggestionDto> Tags { get; set; } = [];
    public IReadOnlyList<DocumentTopicSuggestionDto> Topics { get; set; } = [];
    public int TotalCount { get; set; }
}
