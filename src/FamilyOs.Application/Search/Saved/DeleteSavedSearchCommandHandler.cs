using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Search.Saved;

public sealed class DeleteSavedSearchCommandHandler : IRequestHandler<DeleteSavedSearchCommand>
{
    private readonly IFamilyOsDbContext _db;

    public DeleteSavedSearchCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteSavedSearchCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.SavedSearches
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.UserAccountId == request.UserId,
                cancellationToken)
            ?? throw new NotFoundException("SavedSearch", request.Id);

        _db.SavedSearches.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
