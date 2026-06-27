using MediatR;

namespace FamilyOs.Application.Reminders;

public sealed record DeleteReminderCommand(Guid Id, Guid RequestingUserId) : IRequest;
