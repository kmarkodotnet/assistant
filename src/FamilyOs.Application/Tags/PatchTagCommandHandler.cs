using FamilyOs.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Tags;

public sealed class PatchTagCommandHandler : IRequestHandler<PatchTagCommand>
{
    private readonly IFamilyOsDbContext _db;

    public PatchTagCommandHandler(IFamilyOsDbContext db) => _db = db;

    public async Task Handle(PatchTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tag {request.Id} not found.");

        tag.Update(request.Name, request.Color);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
