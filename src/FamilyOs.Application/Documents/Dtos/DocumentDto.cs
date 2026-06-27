using FamilyOs.Domain.Enums;

namespace FamilyOs.Application.Documents.Dtos;

public record DocumentDto(
    Guid Id,
    string Title,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    string Sha256,
    SourceType SourceType,
    bool IsPrivate,
    ProcessingStatus ProcessingStatus,
    DateOnly? DocumentDate,
    Guid? RelatedFamilyMemberId,
    Guid CreatedByUserAccountId,
    DateTime CreatedUtc,
    DateTime UpdatedUtc
);
