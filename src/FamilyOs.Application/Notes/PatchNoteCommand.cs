using MediatR;

namespace FamilyOs.Application.Notes;

public sealed record PatchNoteCommand(
    Guid Id,
    Guid RequestingUserId,
    string? Title,
    string? Body) : IRequest;
