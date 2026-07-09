using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Documents.GetDocumentClassification;

public record GetDocumentClassificationQuery(Guid DocumentId) : IRequest<DocumentClassificationDto>;

public record DocumentClassificationDto(
    IReadOnlyList<ClassificationTagDto> Tags,
    IReadOnlyList<ClassificationTopicDto> Topics
);

public record ClassificationTagDto(Guid Id, string Name, string? Color, Origin Origin, bool IsApproved);
public record ClassificationTopicDto(Guid Id, string Name, string Slug, string? Icon, Origin Origin, bool IsApproved);
