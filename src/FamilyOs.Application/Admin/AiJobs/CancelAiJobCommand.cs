using MediatR;

namespace FamilyOs.Application.Admin.AiJobs;

public sealed record CancelAiJobCommand(Guid JobId) : IRequest;
