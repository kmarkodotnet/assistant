using MediatR;

namespace FamilyOs.Application.Search.Saved;

public sealed record SaveSearchCommand(string Name, string QueryJson, Guid UserId)
    : IRequest<SavedSearchDto>;
