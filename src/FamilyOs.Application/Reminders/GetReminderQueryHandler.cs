using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Application.Reminders.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Reminders;

public sealed class GetReminderQueryHandler : IRequestHandler<GetReminderQuery, ReminderDto>
{
    private readonly IFamilyOsDbContext _db;

    public GetReminderQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<ReminderDto> Handle(GetReminderQuery request, CancellationToken cancellationToken)
    {
        var reminder = await _db.Reminders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TargetUserAccountId == request.RequestingUserId, cancellationToken)
            ?? throw new NotFoundException("Reminder", request.Id);

        return CreateReminderCommandHandler.MapToDto(reminder);
    }
}
