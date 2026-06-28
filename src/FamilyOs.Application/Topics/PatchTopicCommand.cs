using MediatR;

namespace FamilyOs.Application.Topics;

public sealed record PatchTopicCommand(Guid Id, string? Name, string? Icon, int? SortOrder) : IRequest;
