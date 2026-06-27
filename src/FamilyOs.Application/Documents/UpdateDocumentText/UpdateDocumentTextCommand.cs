using MediatR;

namespace FamilyOs.Application.Documents.UpdateDocumentText;

public record UpdateDocumentTextCommand(Guid DocumentId, string Content) : IRequest;
