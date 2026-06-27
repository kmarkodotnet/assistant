using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Abstractions.Storage;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.DownloadDocument;

public sealed class DownloadDocumentQueryHandler(
    IFamilyOsDbContext db,
    IDocumentStorage storage,
    IFamilyOsAuthorizationService authService,
    ICurrentUserAccessor currentUser,
    IAuditLogger auditLogger)
    : IRequestHandler<DownloadDocumentQuery, DownloadDocumentResult>
{
    public async Task<DownloadDocumentResult> Handle(DownloadDocumentQuery query, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == query.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", query.DocumentId);

        if (!authService.CanReadDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága letölteni ezt a dokumentumot.");

        var stream = await storage.OpenReadAsync(doc.StoragePath, cancellationToken);

        await auditLogger.LogAsync(
            AuditAction.FileAccess,
            currentUser.UserAccountId,
            entityType: "Document",
            entityId: doc.Id,
            ct: cancellationToken);

        return new DownloadDocumentResult(stream, doc.MimeType, doc.OriginalFileName);
    }
}
