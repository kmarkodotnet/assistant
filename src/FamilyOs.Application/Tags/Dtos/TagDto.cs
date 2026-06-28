namespace FamilyOs.Application.Tags.Dtos;

public sealed class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedUtc { get; set; }
}
