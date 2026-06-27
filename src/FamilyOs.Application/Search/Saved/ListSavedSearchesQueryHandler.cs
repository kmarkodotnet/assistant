using FamilyOs.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Search.Saved;

public sealed class ListSavedSearchesQueryHandler
    : IRequestHandler<ListSavedSearchesQuery, IReadOnlyList<SavedSearchDto>>
{
    private readonly IFamilyOsDbContext _db;

    public ListSavedSearchesQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SavedSearchDto>> Handle(
        ListSavedSearchesQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.SavedSearches
            .AsNoTracking()
            .Where(s => s.UserAccountId == request.UserId)
            .OrderByDescending(s => s.CreatedUtc)
            .Select(s => new SavedSearchDto(s.Id, s.Name, s.QueryJson, s.CreatedUtc))
            .ToListAsync(cancellationToken);
    }
}
