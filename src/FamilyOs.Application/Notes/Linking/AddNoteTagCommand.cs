using MediatR;

namespace FamilyOs.Application.Notes.Linking;

public sealed record AddNoteTagCommand(Guid NoteId, Guid TagId, Guid RequestingUserId) : IRequest;
