using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.PatchDocument;

public sealed class PatchDocumentCommandHandler(
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<PatchDocumentCommand>
{
    public async Task Handle(PatchDocumentCommand cmd, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", cmd.DocumentId);

        if (!authService.CanWriteDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága szerkeszteni ezt a dokumentumot.");

        // Optimistic concurrency: xmin is handled by EF Core as row version
        doc.UpdateMetadata(cmd.Title, cmd.DocumentDate, cmd.RelatedFamilyMemberId, cmd.IsPrivate);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("A dokumentumot közben módosították, töltse be újra.");
        }
    }
}
