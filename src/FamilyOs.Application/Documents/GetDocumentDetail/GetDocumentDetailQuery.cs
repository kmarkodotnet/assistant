using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Documents.GetDocumentDetail;

public record GetDocumentDetailQuery(Guid DocumentId) : IRequest<DocumentDetailDto>;

public record DocumentDetailDto(
    Guid Id,
    string Title,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    bool IsPrivate,
    ProcessingStatus ProcessingStatus,
    DateOnly? DocumentDate,
    Guid? RelatedFamilyMemberId,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    DocumentTextSummaryDto? TextSummary,
    string? AiSummary
);

public record DocumentTextSummaryDto(
    int CharCount,
    string? LanguageDetected,
    bool IsManuallyEdited,
    ExtractionMethod ExtractionMethod
);
