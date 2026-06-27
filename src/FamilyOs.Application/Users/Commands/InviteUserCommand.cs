using MediatR;

namespace FamilyOs.Application.Users.Commands;

public record InviteUserCommand(string Email, Guid FamilyMemberId, string Role) : IRequest;
