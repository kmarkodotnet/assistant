using MediatR;

namespace FamilyOs.Application.Family.Commands;

public record DeleteFamilyMemberCommand(Guid Id) : IRequest;
