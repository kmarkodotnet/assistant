using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FamilyOs.Api.Realtime;

[Authorize]
public sealed class DocumentsHub : Hub
{
    // Client methods (called by server):
    // documentProcessingProgress(documentId, stage, percent)
    // documentProcessed(documentId, status)
    // documentFailed(documentId, error)
}
