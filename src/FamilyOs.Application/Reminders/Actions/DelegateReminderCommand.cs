using MediatR;

namespace FamilyOs.Application.Reminders.Actions;

public sealed record DelegateReminderCommand(Guid Id, Guid RequestingUserId, Guid TargetUserAccountId) : IRequest;
