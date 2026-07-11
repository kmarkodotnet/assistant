using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.AddDocumentTag;

/// <summary>
/// ADR-0011 D3 — 1:1 mirrors AddDocumentTopicCommandHandler. Replaces the
/// POST /documents/{id}/tags 501 stub (T-CBE-17).
/// </summary>
public sealed class AddDocumentTagCommandHandler(
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<AddDocumentTagCommand>
{
    public async Task Handle(AddDocumentTagCommand cmd, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", cmd.DocumentId);

        if (!authService.CanWriteDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága szerkeszteni ezt a dokumentumot.");

        var tagExists = await db.Tags.AnyAsync(t => t.Id == cmd.TagId, cancellationToken);
        if (!tagExists)
            throw new NotFoundException("Tag", cmd.TagId);

        var already = await db.DocumentTags
            .AnyAsync(dt => dt.DocumentId == cmd.DocumentId && dt.TagId == cmd.TagId, cancellationToken);
        if (already) return;

        db.DocumentTags.Add(new DocumentTag
        {
            DocumentId = cmd.DocumentId,
            TagId = cmd.TagId,
            Origin = Origin.Manual,
            IsApproved = true,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
