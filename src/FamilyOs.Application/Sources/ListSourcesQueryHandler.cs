using FamilyOs.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Sources;

public sealed class ListSourcesQueryHandler(IFamilyOsDbContext db)
    : IRequestHandler<ListSourcesQuery, IReadOnlyList<SourceDto>>
{
    public async Task<IReadOnlyList<SourceDto>> Handle(ListSourcesQuery request, CancellationToken cancellationToken)
    {
        var sources = await db.Sources
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SourceDto(s.Id, s.Name, s.Kind.ToString(), s.IsActive, s.LastSyncUtc))
            .ToListAsync(cancellationToken);

        return sources;
    }
}
