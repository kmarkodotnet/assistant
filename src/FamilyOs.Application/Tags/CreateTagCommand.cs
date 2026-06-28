using FamilyOs.Application.Tags.Dtos;
using MediatR;

namespace FamilyOs.Application.Tags;

public sealed record CreateTagCommand(string Name, string? Color) : IRequest<TagDto>;
