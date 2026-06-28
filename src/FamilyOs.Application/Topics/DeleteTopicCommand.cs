using MediatR;

namespace FamilyOs.Application.Topics;

public sealed record DeleteTopicCommand(Guid Id) : IRequest;
