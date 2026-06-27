using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Reminders;

public sealed record PatchReminderCommand(
    Guid Id,
    Guid RequestingUserId,
    DateTime? TriggerUtc,
    NotificationChannel? Channel) : IRequest;
