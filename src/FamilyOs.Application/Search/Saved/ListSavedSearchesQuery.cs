using MediatR;

namespace FamilyOs.Application.Search.Saved;

public sealed record SavedSearchDto(Guid Id, string Name, string QueryJson, DateTime CreatedUtc);

public sealed record ListSavedSearchesQuery(Guid UserId) : IRequest<IReadOnlyList<SavedSearchDto>>;
