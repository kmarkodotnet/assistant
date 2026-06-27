using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Tasks.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Tasks;

public sealed class ListTasksQueryHandler : IRequestHandler<ListTasksQuery, IReadOnlyList<TaskListItemDto>>
{
    private readonly IFamilyOsDbContext _db;

    public ListTasksQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TaskListItemDto>> Handle(
        ListTasksQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Tasks
            .AsNoTracking()
            .Where(t => !t.IsPrivate || t.CreatedByUserAccountId == request.UserId);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.AssignedToFamilyMemberId.HasValue)
            query = query.Where(t => t.AssignedToFamilyMemberId == request.AssignedToFamilyMemberId.Value);

        if (request.Priority.HasValue)
            query = query.Where(t => t.Priority == request.Priority.Value);

        if (request.Origin.HasValue)
            query = query.Where(t => t.Origin == request.Origin.Value);

        return await query
            .OrderByDescending(t => t.CreatedUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TaskListItemDto
            {
                Id = t.Id,
                Title = t.Title,
                DueDateUtc = t.DueDateUtc,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                Origin = t.Origin.ToString(),
                AssignedToFamilyMemberId = t.AssignedToFamilyMemberId,
                CreatedUtc = t.CreatedUtc,
            })
            .ToListAsync(cancellationToken);
    }
}
