using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Ai;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Deadlines;

public sealed class PatchDeadlineCommandHandler : IRequestHandler<PatchDeadlineCommand>
{
    private readonly IFamilyOsDbContext _db;
    private readonly IAiProcessingJobRepository _jobRepository;

    public PatchDeadlineCommandHandler(IFamilyOsDbContext db, IAiProcessingJobRepository jobRepository)
    {
        _db = db;
        _jobRepository = jobRepository;
    }

    public async Task Handle(PatchDeadlineCommand request, CancellationToken cancellationToken)
    {
        var deadline = await _db.Deadlines
            .FirstOrDefaultAsync(d => d.Id == request.DeadlineId, cancellationToken)
            ?? throw new NotFoundException("Deadline", request.DeadlineId);

        if (deadline.IsPrivate && deadline.CreatedByUserAccountId != request.UserId)
            throw new ForbiddenException("Nincs jogosultsága szerkeszteni ezt a határidőt.");

        deadline.UpdateDetails(
            request.Title,
            request.Description,
            request.DueDateUtc,
            request.Category,
            request.RelatedFamilyMemberId,
            request.IsPrivate);

        var job = AiProcessingJob.CreateForDeadline(AiJobType.Embed, deadline.Id);
        await _jobRepository.AddAsync(job, cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("A határidőt közben módosították, töltse be újra.");
        }
    }
}
