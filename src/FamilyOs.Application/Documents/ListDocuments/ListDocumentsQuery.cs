using FamilyOs.Application.Documents.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Documents.ListDocuments;

public record ListDocumentsQuery(
    int Page,
    int PageSize,
    Guid? RelatedFamilyMemberId,
    ProcessingStatus? ProcessingStatus
) : IRequest<DocumentListResponse>;

public record DocumentListResponse(
    IReadOnlyList<DocumentDto> Items,
    int Page,
    int PageSize,
    int TotalCount
);
