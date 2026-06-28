using MediatR;

namespace FamilyOs.Application.Sources;

public sealed record ListSourcesQuery : IRequest<IReadOnlyList<SourceDto>>;

public sealed record SourceDto(Guid Id, string Name, string Kind, bool IsActive, DateTime? LastSyncUtc);
