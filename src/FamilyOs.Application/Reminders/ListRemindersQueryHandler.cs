using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Reminders.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Reminders;

public sealed class ListRemindersQueryHandler : IRequestHandler<ListRemindersQuery, ReminderGroupDto>
{
    private readonly IFamilyOsDbContext _db;

    public ListRemindersQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<ReminderGroupDto> Handle(ListRemindersQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = _db.Reminders
            .AsNoTracking()
            .Where(r => r.TargetUserAccountId == request.RequestingUserId);

        if (request.Upcoming)
        {
            query = query.Where(r => r.TriggerUtc <= now.AddDays(30));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        var reminders = await query
            .OrderBy(r => r.TriggerUtc)
            .ToListAsync(cancellationToken);

        var result = new ReminderGroupDto();

        foreach (var r in reminders)
        {
            var dto = CreateReminderCommandHandler.MapToDto(r);

            if (r.Status == ReminderStatus.Fired && r.AcknowledgedUtc == null && r.TriggerUtc < now)
            {
                result.Missed.Add(dto);
            }
            else if (r.TriggerUtc <= now)
            {
                result.Now.Add(dto);
            }
            else if (r.TriggerUtc <= now.AddDays(7))
            {
                result.Week.Add(dto);
            }
            else
            {
                result.Later.Add(dto);
            }
        }

        return result;
    }
}
