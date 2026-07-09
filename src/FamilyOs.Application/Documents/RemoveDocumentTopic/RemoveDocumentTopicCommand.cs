using MediatR;

namespace FamilyOs.Application.Documents.RemoveDocumentTopic;

public record RemoveDocumentTopicCommand(Guid DocumentId, Guid TopicId) : IRequest;
