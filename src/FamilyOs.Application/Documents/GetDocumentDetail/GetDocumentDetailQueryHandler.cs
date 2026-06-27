using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.GetDocumentDetail;

public sealed class GetDocumentDetailQueryHandler(
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<GetDocumentDetailQuery, DocumentDetailDto>
{
    public async Task<DocumentDetailDto> Handle(GetDocumentDetailQuery query, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .AsNoTracking()
            .Include(d => d.DocumentText)
            .FirstOrDefaultAsync(d => d.Id == query.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", query.DocumentId);

        if (!authService.CanReadDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága megtekinteni ezt a dokumentumot.");

        DocumentTextSummaryDto? textSummary = doc.DocumentText is null
            ? null
            : new DocumentTextSummaryDto(
                doc.DocumentText.CharCount,
                doc.DocumentText.LanguageDetected,
                doc.DocumentText.IsManuallyEdited,
                doc.DocumentText.ExtractionMethod);

        return new DocumentDetailDto(
            doc.Id,
            doc.Title,
            doc.OriginalFileName,
            doc.MimeType,
            doc.SizeBytes,
            doc.IsPrivate,
            doc.ProcessingStatus,
            doc.DocumentDate,
            doc.RelatedFamilyMemberId,
            doc.CreatedUtc,
            doc.UpdatedUtc,
            textSummary);
    }
}
