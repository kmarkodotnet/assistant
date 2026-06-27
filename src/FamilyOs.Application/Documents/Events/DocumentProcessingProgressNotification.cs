namespace FamilyOs.Application.Documents.Events;

public record DocumentProcessingProgressNotification(Guid DocumentId, string Stage, int Percent);
