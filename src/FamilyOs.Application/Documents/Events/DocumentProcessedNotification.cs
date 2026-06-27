namespace FamilyOs.Application.Documents.Events;

public record DocumentProcessedNotification(Guid DocumentId, string Status);

public record DocumentFailedNotification(Guid DocumentId, string Error);
