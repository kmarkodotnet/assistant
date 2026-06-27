using MediatR;

namespace FamilyOs.Application.Documents.PatchDocument;

public record PatchDocumentCommand(
    Guid DocumentId,
    string? Title,
    DateOnly? DocumentDate,
    Guid? RelatedFamilyMemberId,
    bool? IsPrivate,
    string? RowVersion
) : IRequest;
