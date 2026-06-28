using FamilyOs.Application.Tags.Dtos;
using MediatR;

namespace FamilyOs.Application.Tags;

public sealed record ListTagsQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize) : IRequest<List<TagDto>>;
