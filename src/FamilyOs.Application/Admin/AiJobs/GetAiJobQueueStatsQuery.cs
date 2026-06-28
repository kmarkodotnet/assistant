using MediatR;

namespace FamilyOs.Application.Admin.AiJobs;

public sealed record GetAiJobQueueStatsQuery : IRequest<IReadOnlyList<QueueStatEntry>>;

public sealed record QueueStatEntry(string JobType, string Status, int Count);
