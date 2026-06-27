using FamilyOs.Application.Reminders.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Reminders;

public sealed record CreateReminderCommand(
    Guid? TaskId,
    Guid? DeadlineId,
    Guid TargetUserAccountId,
    NotificationChannel Channel,
    DateTime TriggerUtc,
    string? RruleExpression,
    Guid CreatedByUserId) : IRequest<ReminderDto>;
