namespace FamilyOs.Application.Topics.Dtos;

public sealed class TopicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedUtc { get; set; }
    public List<TopicDto> Children { get; set; } = [];
}
