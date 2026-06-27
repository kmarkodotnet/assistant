using MediatR;

namespace FamilyOs.Application.Notifications;

public sealed record MarkAllAsReadCommand(Guid UserId) : IRequest;
