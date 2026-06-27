using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Documents.GetDocumentText;

public record GetDocumentTextQuery(Guid DocumentId) : IRequest<DocumentTextDto>;

public record DocumentTextDto(
    string Content,
    ExtractionMethod ExtractionMethod,
    string? LanguageDetected,
    int CharCount,
    bool IsManuallyEdited
);
