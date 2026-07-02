namespace FamilyOs.Domain.Entities;

public sealed class NotificationFeed
{
    private NotificationFeed() { }

    public Guid Id { get; private set; }
    public Guid TargetUserAccountId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Body { get; private set; }
    public string? ActionUrl { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTime? ReadUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    public static NotificationFeed Create(
        Guid userId,
        string type,
        string title,
        string? body = null,
        string? actionUrl = null,
        string? idempotencyKey = null)
        => new()
        {
            Id = Guid.CreateVersion7(),
            TargetUserAccountId = userId,
            Type = type,
            Title = title,
            Body = body,
            ActionUrl = actionUrl,
            IdempotencyKey = idempotencyKey,
            CreatedUtc = DateTime.UtcNow,
        };

    public void MarkRead()
    {
        ReadUtc = DateTime.UtcNow;
    }
}
