using MediatR;

namespace FamilyOs.Application.Notifications;

public sealed record MarkAsReadCommand(Guid NotificationId, Guid UserId) : IRequest;
