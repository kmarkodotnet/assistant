using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Admin.AiJobs;

public sealed class RetryAiJobCommandHandler(IFamilyOsDbContext db)
    : IRequestHandler<RetryAiJobCommand>
{
    public async Task Handle(RetryAiJobCommand request, CancellationToken cancellationToken)
    {
        var job = await db.AiProcessingJobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken)
            ?? throw new NotFoundException("AiProcessingJob", request.JobId);

        job.ResetForRetry();

        await db.SaveChangesAsync(cancellationToken);
    }
}
