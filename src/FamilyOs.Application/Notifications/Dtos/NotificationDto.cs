namespace FamilyOs.Application.Notifications.Dtos;

public sealed class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? ReadUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class NotificationFeedResponse
{
    public List<NotificationDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}
