using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Reminders;

public sealed class DeleteReminderCommandHandler : IRequestHandler<DeleteReminderCommand>
{
    private readonly IFamilyOsDbContext _db;

    public DeleteReminderCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteReminderCommand request, CancellationToken cancellationToken)
    {
        var reminder = await _db.Reminders
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TargetUserAccountId == request.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Reminder", request.Id);

        reminder.Cancel();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
