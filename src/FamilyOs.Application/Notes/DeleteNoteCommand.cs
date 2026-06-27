using MediatR;

namespace FamilyOs.Application.Notes;

public sealed record DeleteNoteCommand(Guid Id, Guid RequestingUserId) : IRequest;
