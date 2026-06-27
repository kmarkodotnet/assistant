using MediatR;

namespace FamilyOs.Application.Reminders.Actions;

public sealed record SkipReminderCommand(Guid Id, Guid RequestingUserId) : IRequest;
