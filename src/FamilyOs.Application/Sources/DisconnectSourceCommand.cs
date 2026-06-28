using FamilyOs.Application.Common.Behaviors;
using MediatR;

namespace FamilyOs.Application.Sources;

[NoAudit]
public sealed record DisconnectSourceCommand(Guid SourceId) : IRequest;
