using MediatR;

namespace FamilyOs.Application.Sources;

public record ConnectGmailCommand(string RefreshToken) : IRequest;
