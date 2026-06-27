using FamilyOs.Application.Documents.Dtos;
using MediatR;

namespace FamilyOs.Application.Documents.UploadDocument;

public record UploadDocumentCommand(
    Stream FileStream,
    string OriginalFileName,
    string? Title,
    DateOnly? DocumentDate,
    Guid? RelatedFamilyMemberId,
    bool IsPrivate
) : IRequest<DocumentDto>;
