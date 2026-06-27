using MediatR;

namespace FamilyOs.Application.Users.Commands;

public record DeleteUserAccountCommand(Guid Id) : IRequest;
