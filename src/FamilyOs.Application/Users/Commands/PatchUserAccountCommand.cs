using MediatR;

namespace FamilyOs.Application.Users.Commands;

public record PatchUserAccountCommand(Guid Id, string? Role, bool? IsActive) : IRequest;
