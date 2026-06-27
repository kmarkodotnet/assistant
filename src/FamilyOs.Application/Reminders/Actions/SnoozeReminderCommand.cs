using MediatR;

namespace FamilyOs.Application.Reminders.Actions;

public sealed record SnoozeReminderCommand(Guid Id, Guid RequestingUserId, int SnoozeMinutes) : IRequest;
