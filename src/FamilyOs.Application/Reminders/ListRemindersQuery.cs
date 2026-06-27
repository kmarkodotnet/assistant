using FamilyOs.Application.Reminders.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Reminders;

public sealed record ListRemindersQuery(
    Guid RequestingUserId,
    bool Upcoming,
    ReminderStatus? Status) : IRequest<ReminderGroupDto>;
