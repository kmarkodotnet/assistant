using MediatR;

namespace FamilyOs.Application.Documents.AddDocumentTag;

public record AddDocumentTagCommand(Guid DocumentId, Guid TagId) : IRequest;
