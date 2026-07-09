using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.AddDocumentTopic;

public sealed class AddDocumentTopicCommandHandler(
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<AddDocumentTopicCommand>
{
    public async Task Handle(AddDocumentTopicCommand cmd, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", cmd.DocumentId);

        if (!authService.CanWriteDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága szerkeszteni ezt a dokumentumot.");

        var topicExists = await db.Topics.AnyAsync(t => t.Id == cmd.TopicId, cancellationToken);
        if (!topicExists)
            throw new NotFoundException("Topic", cmd.TopicId);

        var already = await db.DocumentTopics
            .AnyAsync(dt => dt.DocumentId == cmd.DocumentId && dt.TopicId == cmd.TopicId, cancellationToken);
        if (already) return;

        db.DocumentTopics.Add(new DocumentTopic
        {
            DocumentId = cmd.DocumentId,
            TopicId = cmd.TopicId,
            Origin = Origin.Manual,
            IsApproved = true,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
