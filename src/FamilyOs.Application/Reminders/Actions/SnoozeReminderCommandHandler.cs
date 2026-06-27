using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Reminders.Actions;

public sealed class SnoozeReminderCommandHandler : IRequestHandler<SnoozeReminderCommand>
{
    private readonly IFamilyOsDbContext _db;

    public SnoozeReminderCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(SnoozeReminderCommand request, CancellationToken cancellationToken)
    {
        var reminder = await _db.Reminders
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TargetUserAccountId == request.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Reminder", request.Id);

        var snoozedTrigger = DateTime.UtcNow.AddMinutes(request.SnoozeMinutes);

        Reminder snoozed;
        if (reminder.TaskId.HasValue)
        {
            snoozed = Reminder.ForTask(
                reminder.TaskId.Value,
                reminder.TargetUserAccountId,
                snoozedTrigger,
                reminder.Channel,
                reminder.CreatedByUserAccountId);
        }
        else
        {
            snoozed = Reminder.ForDeadline(
                reminder.DeadlineId!.Value,
                reminder.TargetUserAccountId,
                snoozedTrigger,
                reminder.Channel,
                reminder.CreatedByUserAccountId);
        }

        _db.Reminders.Add(snoozed);
        reminder.Cancel();

        await _db.SaveChangesAsync(cancellationToken);
    }
}
