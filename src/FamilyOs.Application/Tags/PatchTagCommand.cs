using MediatR;

namespace FamilyOs.Application.Tags;

public sealed record PatchTagCommand(Guid Id, string? Name, string? Color) : IRequest;
