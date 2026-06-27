using FamilyOs.Application.Common.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notes;

public sealed class PatchNoteCommandHandler : IRequestHandler<PatchNoteCommand>
{
    private readonly IFamilyOsDbContext _db;
    private readonly IAiProcessingJobRepository _jobRepository;

    public PatchNoteCommandHandler(IFamilyOsDbContext db, IAiProcessingJobRepository jobRepository)
    {
        _db = db;
        _jobRepository = jobRepository;
    }

    public async Task Handle(PatchNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _db.Notes
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Note", request.Id);

        if (note.CreatedByUserAccountId != request.RequestingUserId)
            throw new ForbiddenException("Csak a saját jegyzetét módosíthatja.");

        var bodyChanged = request.Body is not null && request.Body != note.Body;

        note.UpdateContent(
            request.Title ?? note.Title,
            request.Body ?? note.Body);

        if (bodyChanged)
        {
            var job = AiProcessingJob.CreateForNote(AiJobType.Embed, note.Id);
            await _jobRepository.AddAsync(job, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
