using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Reminders;

public sealed class PatchReminderCommandHandler : IRequestHandler<PatchReminderCommand>
{
    private readonly IFamilyOsDbContext _db;

    public PatchReminderCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(PatchReminderCommand request, CancellationToken cancellationToken)
    {
        var reminder = await _db.Reminders
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Reminder", request.Id);

        if (request.TriggerUtc.HasValue || request.Channel.HasValue)
        {
            reminder.UpdateTrigger(
                request.TriggerUtc ?? reminder.TriggerUtc,
                request.Channel ?? reminder.Channel);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
