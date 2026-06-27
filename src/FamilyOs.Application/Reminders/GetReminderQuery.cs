using FamilyOs.Application.Reminders.Dtos;
using MediatR;

namespace FamilyOs.Application.Reminders;

public sealed record GetReminderQuery(Guid Id, Guid RequestingUserId) : IRequest<ReminderDto>;
