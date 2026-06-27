using MediatR;

namespace FamilyOs.Application.Suggestions;

public sealed record GetSuggestionsQuery(Guid? UserId) : IRequest<SuggestionsAggregateDto>;
