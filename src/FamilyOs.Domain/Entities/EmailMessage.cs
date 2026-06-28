using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class EmailMessage
{
    private EmailMessage() { }

    public Guid Id { get; private set; }
    public Guid SourceId { get; private set; }
    public string GmailMessageId { get; private set; } = string.Empty;
    public string? ThreadId { get; private set; }
    public string FromAddress { get; private set; } = string.Empty;
    public string ToAddresses { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public DateTime ReceivedUtc { get; private set; }
    public string? BodyText { get; private set; }
    public string? BodyHtml { get; private set; }
    public string? Snippet { get; private set; }
    public bool HasAttachments { get; private set; }
    public IngestStatus IngestStatus { get; private set; }
    public DateTime? ProcessedUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static EmailMessage Create(
        Guid sourceId,
        string gmailMessageId,
        string fromAddress,
        string toAddresses,
        string subject,
        DateTime receivedUtc,
        bool hasAttachments)
        => new()
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            GmailMessageId = gmailMessageId,
            FromAddress = fromAddress,
            ToAddresses = toAddresses,
            Subject = subject,
            ReceivedUtc = receivedUtc,
            HasAttachments = hasAttachments,
            IngestStatus = IngestStatus.Pending,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

    public void SetBody(string? bodyText, string? bodyHtml, string? snippet)
    {
        BodyText = bodyText;
        BodyHtml = bodyHtml;
        Snippet = snippet;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkProcessed()
    {
        IngestStatus = IngestStatus.Processed;
        ProcessedUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        IngestStatus = IngestStatus.Failed;
        UpdatedUtc = DateTime.UtcNow;
    }
}
