using FamilyOs.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Tags;

public sealed class DeleteTagCommandHandler : IRequestHandler<DeleteTagCommand>
{
    private readonly IFamilyOsDbContext _db;

    public DeleteTagCommandHandler(IFamilyOsDbContext db) => _db = db;

    public async Task Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tag {request.Id} not found.");

        if (tag.UsageCount > 0 && !request.Force)
            throw new InvalidOperationException($"Tag '{tag.Name}' has {tag.UsageCount} usages. Use force=true to delete.");

        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
