namespace FamilyOs.Application.Abstractions.Ai;

public interface IProcessingProgressNotifier
{
    Task NotifyProgressAsync(Guid documentId, string stage, int percent, CancellationToken ct = default);
    Task NotifyProcessedAsync(Guid documentId, string status, CancellationToken ct = default);
    Task NotifyFailedAsync(Guid documentId, string errorMessage, CancellationToken ct = default);
}
