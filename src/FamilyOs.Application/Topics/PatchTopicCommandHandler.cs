using FamilyOs.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Topics;

public sealed class PatchTopicCommandHandler : IRequestHandler<PatchTopicCommand>
{
    private readonly IFamilyOsDbContext _db;

    public PatchTopicCommandHandler(IFamilyOsDbContext db) => _db = db;

    public async Task Handle(PatchTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Téma {request.Id} nem található.");

        topic.Update(request.Name, request.Icon, request.SortOrder);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
