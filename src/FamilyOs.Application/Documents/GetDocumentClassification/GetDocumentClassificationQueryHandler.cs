using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.GetDocumentClassification;

public sealed class GetDocumentClassificationQueryHandler(
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<GetDocumentClassificationQuery, DocumentClassificationDto>
{
    public async Task<DocumentClassificationDto> Handle(GetDocumentClassificationQuery query, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == query.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", query.DocumentId);

        if (!authService.CanReadDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága megtekinteni ezt a dokumentumot.");

        var tags = await db.DocumentTags
            .AsNoTracking()
            .Include(dt => dt.Tag)
            .Where(dt => dt.DocumentId == query.DocumentId && dt.Tag != null)
            .Select(dt => new ClassificationTagDto(dt.Tag!.Id, dt.Tag.Name, dt.Tag.Color, dt.Origin, dt.IsApproved))
            .ToListAsync(cancellationToken);

        var topics = await db.DocumentTopics
            .AsNoTracking()
            .Include(dt => dt.Topic)
            .Where(dt => dt.DocumentId == query.DocumentId && dt.Topic != null)
            .Select(dt => new ClassificationTopicDto(dt.Topic!.Id, dt.Topic.Name, dt.Topic.Slug, dt.Topic.Icon, dt.Origin, dt.IsApproved))
            .ToListAsync(cancellationToken);

        return new DocumentClassificationDto(tags, topics);
    }
}
