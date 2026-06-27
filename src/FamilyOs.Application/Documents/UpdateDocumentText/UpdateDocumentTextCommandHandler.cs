using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.UpdateDocumentText;

public sealed class UpdateDocumentTextCommandHandler(
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<UpdateDocumentTextCommand>
{
    public async Task Handle(UpdateDocumentTextCommand cmd, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .Include(d => d.DocumentText)
            .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", cmd.DocumentId);

        if (!authService.CanWriteDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága szerkeszteni ezt a dokumentumot.");

        if (doc.DocumentText is null)
            throw new NotFoundException("DocumentText", cmd.DocumentId);

        doc.DocumentText.CorrectManually(cmd.Content);
        await db.SaveChangesAsync(cancellationToken);
    }
}
