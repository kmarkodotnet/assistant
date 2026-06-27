using MediatR;

namespace FamilyOs.Application.Reminders.Actions;

public sealed record AcknowledgeReminderCommand(Guid Id, Guid RequestingUserId) : IRequest;
