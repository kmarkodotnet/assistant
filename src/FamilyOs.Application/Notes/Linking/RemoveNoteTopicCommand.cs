using MediatR;

namespace FamilyOs.Application.Notes.Linking;

public sealed record RemoveNoteTopicCommand(Guid NoteId, Guid TopicId, Guid RequestingUserId) : IRequest;
