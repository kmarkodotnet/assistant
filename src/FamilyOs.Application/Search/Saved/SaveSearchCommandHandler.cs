using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Domain.Entities;
using MediatR;

namespace FamilyOs.Application.Search.Saved;

public sealed class SaveSearchCommandHandler : IRequestHandler<SaveSearchCommand, SavedSearchDto>
{
    private readonly IFamilyOsDbContext _db;

    public SaveSearchCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<SavedSearchDto> Handle(
        SaveSearchCommand request,
        CancellationToken cancellationToken)
    {
        var entity = SavedSearch.Create(request.Name, request.QueryJson, request.UserId);
        _db.SavedSearches.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new SavedSearchDto(entity.Id, entity.Name, entity.QueryJson, entity.CreatedUtc);
    }
}
