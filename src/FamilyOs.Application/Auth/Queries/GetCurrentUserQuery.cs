using FamilyOs.Application.Auth.Dtos;
using MediatR;

namespace FamilyOs.Application.Auth.Queries;

public record GetCurrentUserQuery : IRequest<CurrentUserDto>;
