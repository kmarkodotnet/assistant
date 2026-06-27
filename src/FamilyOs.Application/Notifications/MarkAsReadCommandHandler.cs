using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notifications;

public sealed class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand>
{
    private readonly IFamilyOsDbContext _db;

    public MarkAsReadCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _db.NotificationFeed
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.TargetUserAccountId == request.UserId, cancellationToken)
            ?? throw new NotFoundException("Notification", request.NotificationId);

        notification.MarkRead();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
