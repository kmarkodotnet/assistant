using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.RemoveDocumentTopic;

public sealed class RemoveDocumentTopicCommandHandler(
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<RemoveDocumentTopicCommand>
{
    public async Task Handle(RemoveDocumentTopicCommand cmd, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", cmd.DocumentId);

        if (!authService.CanWriteDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága szerkeszteni ezt a dokumentumot.");

        var link = await db.DocumentTopics
            .FirstOrDefaultAsync(dt => dt.DocumentId == cmd.DocumentId && dt.TopicId == cmd.TopicId, cancellationToken);

        if (link is null) return;

        db.DocumentTopics.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
    }
}
