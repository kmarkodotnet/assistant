using FamilyOs.Application.Users.Dtos;
using MediatR;

namespace FamilyOs.Application.Users.Queries;

public record GetUserAccountsQuery : IRequest<IReadOnlyList<UserAccountDto>>;
