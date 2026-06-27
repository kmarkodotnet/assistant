using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Reminders.Actions;

public sealed class SkipReminderCommandHandler : IRequestHandler<SkipReminderCommand>
{
    private readonly IFamilyOsDbContext _db;

    public SkipReminderCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(SkipReminderCommand request, CancellationToken cancellationToken)
    {
        var reminder = await _db.Reminders
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TargetUserAccountId == request.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Reminder", request.Id);

        reminder.Skip();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
