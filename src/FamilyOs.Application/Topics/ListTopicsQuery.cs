using FamilyOs.Application.Topics.Dtos;
using MediatR;

namespace FamilyOs.Application.Topics;

public sealed record ListTopicsQuery(bool Flat) : IRequest<List<TopicDto>>;
