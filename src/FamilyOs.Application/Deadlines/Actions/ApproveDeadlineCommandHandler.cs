using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Deadlines.Actions;

public sealed class ApproveDeadlineCommandHandler : IRequestHandler<ApproveDeadlineCommand>
{
    private readonly IFamilyOsDbContext _db;

    public ApproveDeadlineCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(ApproveDeadlineCommand request, CancellationToken cancellationToken)
    {
        var deadline = await _db.Deadlines
            .FirstOrDefaultAsync(d => d.Id == request.DeadlineId, cancellationToken)
            ?? throw new NotFoundException("Deadline", request.DeadlineId);

        // Approve: set Origin=AiApproved and record who approved
        deadline.Approve(request.ApprovedByUserId);

        await _db.SaveChangesAsync(cancellationToken);

        // Epic G: Create Reminder entities based on DefaultReminderPolicy
        var offsets = DefaultReminderPolicy.GetOffsets(deadline.Category);
        foreach (var (offsetDays, _) in offsets)
        {
            var triggerUtc = ReminderTriggerCalculator.CalculateFromOffsetDays(deadline.DueDateUtc, offsetDays);
            if (triggerUtc > DateTime.UtcNow)
            {
                var reminder = Reminder.ForDeadline(
                    deadline.Id,
                    request.ApprovedByUserId,
                    triggerUtc,
                    NotificationChannel.InApp,
                    request.ApprovedByUserId);
                _db.Reminders.Add(reminder);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
