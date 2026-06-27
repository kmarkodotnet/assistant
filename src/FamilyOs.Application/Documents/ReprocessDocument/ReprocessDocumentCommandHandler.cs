using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.ReprocessDocument;

public sealed class ReprocessDocumentCommandHandler(
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<ReprocessDocumentCommand, ReprocessResult>
{
    public async Task<ReprocessResult> Handle(ReprocessDocumentCommand cmd, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", cmd.DocumentId);

        if (!authService.CanWriteDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága újrafeldolgozni ezt a dokumentumot.");

        doc.SetProcessingStatus(ProcessingStatus.Pending);
        await db.SaveChangesAsync(cancellationToken);

        // AI job scheduling is Epic D — return empty list for now
        return new ReprocessResult(Array.Empty<Guid>());
    }
}
