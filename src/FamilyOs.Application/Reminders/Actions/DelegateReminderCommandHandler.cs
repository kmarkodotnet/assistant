using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Reminders.Actions;

public sealed class DelegateReminderCommandHandler : IRequestHandler<DelegateReminderCommand>
{
    private readonly IFamilyOsDbContext _db;

    public DelegateReminderCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DelegateReminderCommand request, CancellationToken cancellationToken)
    {
        var reminder = await _db.Reminders
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TargetUserAccountId == request.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Reminder", request.Id);

        Reminder delegated;
        if (reminder.TaskId.HasValue)
        {
            delegated = Reminder.ForTask(
                reminder.TaskId.Value,
                request.TargetUserAccountId,
                reminder.TriggerUtc,
                reminder.Channel,
                request.RequestingUserId);
        }
        else
        {
            delegated = Reminder.ForDeadline(
                reminder.DeadlineId!.Value,
                request.TargetUserAccountId,
                reminder.TriggerUtc,
                reminder.Channel,
                request.RequestingUserId);
        }

        _db.Reminders.Add(delegated);
        reminder.Cancel();

        await _db.SaveChangesAsync(cancellationToken);
    }
}
