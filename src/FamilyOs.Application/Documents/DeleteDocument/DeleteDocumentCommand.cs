using MediatR;

namespace FamilyOs.Application.Documents.DeleteDocument;

public record DeleteDocumentCommand(Guid DocumentId, bool Hard) : IRequest;
