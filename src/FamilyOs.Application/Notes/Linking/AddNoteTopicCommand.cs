using MediatR;

namespace FamilyOs.Application.Notes.Linking;

public sealed record AddNoteTopicCommand(Guid NoteId, Guid TopicId, Guid RequestingUserId) : IRequest;
