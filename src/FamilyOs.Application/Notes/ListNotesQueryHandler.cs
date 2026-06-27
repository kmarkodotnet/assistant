using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Notes.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notes;

public sealed class ListNotesQueryHandler : IRequestHandler<ListNotesQuery, List<NoteListItemDto>>
{
    private readonly IFamilyOsDbContext _db;

    public ListNotesQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<List<NoteListItemDto>> Handle(ListNotesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Notes
            .AsNoTracking()
            .Where(n => !n.IsPrivate || n.CreatedByUserAccountId == request.RequestingUserId);

        if (request.RelatedFamilyMemberId.HasValue)
        {
            query = query.Where(n => n.RelatedFamilyMemberId == request.RelatedFamilyMemberId.Value);
        }

        if (request.TagId.HasValue)
        {
            query = query.Where(n => n.NoteTags.Any(nt => nt.TagId == request.TagId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.TopicSlug))
        {
            query = query.Where(n => n.NoteTopics.Any(nt => nt.Topic!.Slug == request.TopicSlug));
        }

        return await query
            .OrderByDescending(n => n.UpdatedUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NoteListItemDto
            {
                Id = n.Id,
                Title = n.Title,
                RelatedFamilyMemberId = n.RelatedFamilyMemberId,
                CreatedByUserAccountId = n.CreatedByUserAccountId,
                IsPrivate = n.IsPrivate,
                CreatedUtc = n.CreatedUtc,
                UpdatedUtc = n.UpdatedUtc,
            })
            .ToListAsync(cancellationToken);
    }
}
