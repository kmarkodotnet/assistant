using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.ReprocessDocument;

public sealed class ReprocessDocumentCommandHandler(
    IFamilyOsDbContext db,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<ReprocessDocumentCommand, ReprocessResult>
{
    public async Task<ReprocessResult> Handle(ReprocessDocumentCommand cmd, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", cmd.DocumentId);

        if (!authService.CanWriteDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága újrafeldolgozni ezt a dokumentumot.");

        doc.SetProcessingStatus(ProcessingStatus.Pending);

        // Cancel any existing non-terminal jobs for this document
        var existingJobs = await db.AiProcessingJobs
            .Where(j => j.TargetId == cmd.DocumentId
                        && (j.Status == JobStatus.Queued || j.Status == JobStatus.Failed || j.Status == JobStatus.Running))
            .ToListAsync(cancellationToken);

        foreach (var job in existingJobs)
            job.Cancel();

        // Enqueue a fresh ExtractText job to restart the pipeline
        var extractJob = AiProcessingJob.Create(AiJobType.ExtractText, doc.Id);
        db.AiProcessingJobs.Add(extractJob);

        await db.SaveChangesAsync(cancellationToken);

        return new ReprocessResult([extractJob.Id]);
    }
}
