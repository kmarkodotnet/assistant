using FamilyOs.Application.Topics.Dtos;
using MediatR;

namespace FamilyOs.Application.Topics;

public sealed record CreateTopicCommand(
    string Name,
    string Slug,
    Guid? ParentId,
    string? Icon,
    int SortOrder) : IRequest<TopicDto>;
