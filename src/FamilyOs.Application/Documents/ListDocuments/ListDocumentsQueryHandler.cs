using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Documents.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.ListDocuments;

public sealed class ListDocumentsQueryHandler(
    IFamilyOsDbContext db,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<ListDocumentsQuery, DocumentListResponse>
{
    public async Task<DocumentListResponse> Handle(ListDocumentsQuery query, CancellationToken cancellationToken)
    {
        var q = db.Documents.AsNoTracking();

        // RBAC: Child can only see documents related to their FamilyMember, non-private
        if (currentUser.Role == nameof(UserRole.Child))
        {
            var familyMemberId = currentUser.FamilyMemberId;
            q = q.Where(d => d.RelatedFamilyMemberId == familyMemberId && !d.IsPrivate);
        }

        if (query.RelatedFamilyMemberId.HasValue)
            q = q.Where(d => d.RelatedFamilyMemberId == query.RelatedFamilyMemberId);

        if (query.ProcessingStatus.HasValue)
            q = q.Where(d => d.ProcessingStatus == query.ProcessingStatus);

        var total = await q.CountAsync(cancellationToken);
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 50 : query.PageSize;

        var items = await q
            .OrderByDescending(d => d.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentDto(
                d.Id,
                d.Title,
                d.OriginalFileName,
                d.MimeType,
                d.SizeBytes,
                d.Sha256,
                d.SourceType,
                d.IsPrivate,
                d.ProcessingStatus,
                d.DocumentDate,
                d.RelatedFamilyMemberId,
                d.CreatedByUserAccountId,
                d.CreatedUtc,
                d.UpdatedUtc))
            .ToListAsync(cancellationToken);

        return new DocumentListResponse(items, page, pageSize, total);
    }
}
