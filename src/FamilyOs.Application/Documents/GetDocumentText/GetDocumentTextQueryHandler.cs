using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.GetDocumentText;

public sealed class GetDocumentTextQueryHandler(
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<GetDocumentTextQuery, DocumentTextDto>
{
    public async Task<DocumentTextDto> Handle(GetDocumentTextQuery query, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .AsNoTracking()
            .Include(d => d.DocumentText)
            .FirstOrDefaultAsync(d => d.Id == query.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", query.DocumentId);

        if (!authService.CanReadDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága megtekinteni ezt a dokumentumot.");

        if (doc.DocumentText is null)
            throw new NotFoundException("DocumentText", query.DocumentId);

        return new DocumentTextDto(
            doc.DocumentText.Content,
            doc.DocumentText.ExtractionMethod,
            doc.DocumentText.LanguageDetected,
            doc.DocumentText.CharCount,
            doc.DocumentText.IsManuallyEdited);
    }
}
