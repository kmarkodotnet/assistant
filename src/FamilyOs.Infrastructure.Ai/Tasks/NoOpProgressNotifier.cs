using FamilyOs.Application.Abstractions.Ai;

namespace FamilyOs.Infrastructure.Ai.Tasks;

/// <summary>
/// No-op implementation of IProcessingProgressNotifier for Workers (cross-process SignalR is post-MVP).
/// </summary>
public sealed class NoOpProgressNotifier : IProcessingProgressNotifier
{
    public Task NotifyProgressAsync(Guid documentId, string stage, int percent, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task NotifyProcessedAsync(Guid documentId, string status, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task NotifyFailedAsync(Guid documentId, string errorMessage, CancellationToken ct = default)
        => Task.CompletedTask;
}
