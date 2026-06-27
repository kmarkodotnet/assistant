using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Deadlines.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Deadlines;

public sealed class ListDeadlinesQueryHandler
    : IRequestHandler<ListDeadlinesQuery, IReadOnlyList<DeadlineListItemDto>>
{
    private readonly IFamilyOsDbContext _db;

    public ListDeadlinesQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DeadlineListItemDto>> Handle(
        ListDeadlinesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Deadlines
            .AsNoTracking()
            .Where(d => !d.IsPrivate || d.CreatedByUserAccountId == request.UserId);

        if (request.From.HasValue)
            query = query.Where(d => d.DueDateUtc >= request.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        if (request.To.HasValue)
            query = query.Where(d => d.DueDateUtc <= request.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        if (request.Category.HasValue)
            query = query.Where(d => d.Category == request.Category.Value);

        if (request.Status.HasValue)
            query = query.Where(d => d.Status == request.Status.Value);

        return await query
            .OrderBy(d => d.DueDateUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DeadlineListItemDto
            {
                Id = d.Id,
                Title = d.Title,
                DueDateUtc = d.DueDateUtc,
                Status = d.Status.ToString(),
                Category = d.Category.ToString(),
                Origin = d.Origin.ToString(),
                RelatedFamilyMemberId = d.RelatedFamilyMemberId,
                CreatedUtc = d.CreatedUtc,
            })
            .ToListAsync(cancellationToken);
    }
}
