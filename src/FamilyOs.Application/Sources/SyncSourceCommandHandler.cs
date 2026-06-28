using FamilyOs.Application.Abstractions.Email;
using MediatR;

namespace FamilyOs.Application.Sources;

public sealed class SyncSourceCommandHandler(IEmailIngestionService emailIngestionService)
    : IRequestHandler<SyncSourceCommand, EmailIngestionReport>
{
    public async Task<EmailIngestionReport> Handle(SyncSourceCommand request, CancellationToken cancellationToken)
    {
        return await emailIngestionService.SyncAsync(request.SourceId, cancellationToken);
    }
}
