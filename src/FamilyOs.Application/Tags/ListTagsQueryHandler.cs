using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Tags.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Tags;

public sealed class ListTagsQueryHandler : IRequestHandler<ListTagsQuery, List<TagDto>>
{
    private readonly IFamilyOsDbContext _db;

    public ListTagsQueryHandler(IFamilyOsDbContext db) => _db = db;

    public async Task<List<TagDto>> Handle(ListTagsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Tags.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLowerInvariant();
            query = query.Where(t => t.Name.Contains(search));
        }

        query = request.Sort switch
        {
            "usageCount:desc" => query.OrderByDescending(t => t.UsageCount).ThenBy(t => t.Name),
            "usageCount:asc"  => query.OrderBy(t => t.UsageCount).ThenBy(t => t.Name),
            _                  => query.OrderBy(t => t.Name),
        };

        return await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
                UsageCount = t.UsageCount,
                CreatedUtc = t.CreatedUtc,
            })
            .ToListAsync(cancellationToken);
    }
}
