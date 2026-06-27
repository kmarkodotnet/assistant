using FamilyOs.Application.Auth.Dtos;
using MediatR;

namespace FamilyOs.Application.Auth.Commands;

public record LoginGoogleCommand(string IdToken) : IRequest<CurrentUserDto>;
