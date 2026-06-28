using FamilyOs.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Topics;

public sealed class DeleteTopicCommandHandler : IRequestHandler<DeleteTopicCommand>
{
    private readonly IFamilyOsDbContext _db;

    public DeleteTopicCommandHandler(IFamilyOsDbContext db) => _db = db;

    public async Task Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await _db.Topics
            .Include(t => t.Children)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Téma {request.Id} nem található.");

        if (topic.Children.Count > 0)
            throw new InvalidOperationException("A témának altémái vannak. Törölje azokat először.");

        _db.Topics.Remove(topic);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
