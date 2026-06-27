using MediatR;

namespace FamilyOs.Application.Notes.Linking;

public sealed record RemoveNoteTagCommand(Guid NoteId, Guid TagId, Guid RequestingUserId) : IRequest;
