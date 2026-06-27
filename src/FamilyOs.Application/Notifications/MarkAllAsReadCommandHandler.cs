using FamilyOs.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notifications;

public sealed class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand>
{
    private readonly IFamilyOsDbContext _db;

    public MarkAllAsReadCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        var unread = await _db.NotificationFeed
            .Where(n => n.TargetUserAccountId == request.UserId && n.ReadUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.MarkRead();
        }

        if (unread.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
