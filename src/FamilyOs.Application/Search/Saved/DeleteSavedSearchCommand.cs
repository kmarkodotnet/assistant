using MediatR;

namespace FamilyOs.Application.Search.Saved;

public sealed record DeleteSavedSearchCommand(Guid Id, Guid UserId) : IRequest;
