using MediatR;

namespace FamilyOs.Application.Documents.AddDocumentTopic;

public record AddDocumentTopicCommand(Guid DocumentId, Guid TopicId) : IRequest;
