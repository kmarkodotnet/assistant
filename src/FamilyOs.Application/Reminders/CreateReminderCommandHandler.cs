using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Reminders.Dtos;
using FamilyOs.Domain.Entities;
using MediatR;

namespace FamilyOs.Application.Reminders;

public sealed class CreateReminderCommandHandler : IRequestHandler<CreateReminderCommand, ReminderDto>
{
    private readonly IFamilyOsDbContext _db;

    public CreateReminderCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<ReminderDto> Handle(CreateReminderCommand request, CancellationToken cancellationToken)
    {
        Reminder reminder;

        if (request.TaskId.HasValue)
        {
            reminder = Reminder.ForTask(
                request.TaskId.Value,
                request.TargetUserAccountId,
                request.TriggerUtc,
                request.Channel,
                request.CreatedByUserId,
                request.RruleExpression);
        }
        else
        {
            reminder = Reminder.ForDeadline(
                request.DeadlineId!.Value,
                request.TargetUserAccountId,
                request.TriggerUtc,
                request.Channel,
                request.CreatedByUserId,
                request.RruleExpression);
        }

        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(reminder);
    }

    internal static ReminderDto MapToDto(Reminder r) => new()
    {
        Id = r.Id,
        TaskId = r.TaskId,
        DeadlineId = r.DeadlineId,
        TargetUserAccountId = r.TargetUserAccountId,
        Channel = r.Channel.ToString(),
        Status = r.Status.ToString(),
        TriggerUtc = r.TriggerUtc,
        FiredUtc = r.FiredUtc,
        AcknowledgedUtc = r.AcknowledgedUtc,
        RruleExpression = r.RruleExpression,
        EscalationLevel = r.EscalationLevel,
        SnoozeNote = r.SnoozeNote,
        CreatedByUserAccountId = r.CreatedByUserAccountId,
        CreatedUtc = r.CreatedUtc,
        UpdatedUtc = r.UpdatedUtc,
    };
}
