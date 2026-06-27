using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Reminders.Actions;

public sealed class AcknowledgeReminderCommandHandler : IRequestHandler<AcknowledgeReminderCommand>
{
    private readonly IFamilyOsDbContext _db;

    public AcknowledgeReminderCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AcknowledgeReminderCommand request, CancellationToken cancellationToken)
    {
        var reminder = await _db.Reminders
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TargetUserAccountId == request.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Reminder", request.Id);

        reminder.Acknowledge();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
