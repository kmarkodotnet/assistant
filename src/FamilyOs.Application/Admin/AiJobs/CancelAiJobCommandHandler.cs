using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Admin.AiJobs;

public sealed class CancelAiJobCommandHandler(IFamilyOsDbContext db)
    : IRequestHandler<CancelAiJobCommand>
{
    public async Task Handle(CancelAiJobCommand request, CancellationToken cancellationToken)
    {
        var job = await db.AiProcessingJobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken)
            ?? throw new NotFoundException("AiProcessingJob", request.JobId);

        job.Cancel();

        await db.SaveChangesAsync(cancellationToken);
    }
}
