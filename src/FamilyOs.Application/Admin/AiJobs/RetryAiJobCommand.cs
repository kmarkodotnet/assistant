using MediatR;

namespace FamilyOs.Application.Admin.AiJobs;

public sealed record RetryAiJobCommand(Guid JobId) : IRequest;
