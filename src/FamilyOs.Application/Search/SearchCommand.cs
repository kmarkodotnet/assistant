using FamilyOs.Application.Search.Dtos;
using MediatR;

namespace FamilyOs.Application.Search;

public sealed record SearchCommand(
    SearchRequest Request,
    Guid? UserId,
    Guid? UserFamilyMemberId,
    string? UserRole
) : IRequest<SearchResponse>;
