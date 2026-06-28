using FamilyOs.Application.Abstractions.Email;
using FamilyOs.Application.Common.Behaviors;
using MediatR;

namespace FamilyOs.Application.Sources;

[NoAudit]
public sealed record SyncSourceCommand(Guid SourceId) : IRequest<EmailIngestionReport>;
