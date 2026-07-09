using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Abstractions.Storage;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Documents.DeleteDocument;

public sealed class DeleteDocumentCommandHandler(
    IFamilyOsDbContext db,
    IDocumentStorage storage,
    IFamilyOsAuthorizationService authService)
    : IRequestHandler<DeleteDocumentCommand>
{
    public async Task Handle(DeleteDocumentCommand cmd, CancellationToken cancellationToken)
    {
        var doc = await db.Documents
            .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId, cancellationToken)
            ?? throw new NotFoundException("Document", cmd.DocumentId);

        if (!authService.CanWriteDocument(doc))
            throw new ForbiddenException("Nincs jogosultsága törölni ezt a dokumentumot.");

        // Cancel any active jobs so workers skip them, then delete all job records.
        var jobs = await db.AiProcessingJobs
            .Where(j => j.TargetId == cmd.DocumentId && j.TargetType == JobTargetType.Document)
            .ToListAsync(cancellationToken);

        foreach (var job in jobs.Where(j => j.Status is JobStatus.Queued or JobStatus.Running or JobStatus.Failed))
            job.Cancel();

        await db.SaveChangesAsync(cancellationToken);
        db.AiProcessingJobs.RemoveRange(jobs);

        if (cmd.Hard)
        {
            await storage.DeleteAsync(doc.StoragePath, cancellationToken);
            db.Documents.Remove(doc);
        }
        else
        {
            doc.SoftDelete();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
