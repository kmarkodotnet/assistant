using FamilyOs.Application.Abstractions.Ai;
using Microsoft.AspNetCore.SignalR;

namespace FamilyOs.Api.Realtime;

public sealed class SignalRProgressNotifier : IProcessingProgressNotifier
{
    private readonly IHubContext<DocumentsHub> _hubContext;

    public SignalRProgressNotifier(IHubContext<DocumentsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyProgressAsync(Guid documentId, string stage, int percent, CancellationToken ct = default)
        => _hubContext.Clients.All.SendAsync(
            "documentProcessingProgress",
            new { documentId, stage, percent },
            ct);

    public Task NotifyProcessedAsync(Guid documentId, string status, CancellationToken ct = default)
        => _hubContext.Clients.All.SendAsync(
            "documentProcessed",
            new { documentId, status },
            ct);

    public Task NotifyFailedAsync(Guid documentId, string errorMessage, CancellationToken ct = default)
        => _hubContext.Clients.All.SendAsync(
            "documentFailed",
            new { documentId, error = errorMessage },
            ct);
}
