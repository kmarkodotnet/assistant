using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Deadlines;

public sealed class PatchDeadlineCommandHandler : IRequestHandler<PatchDeadlineCommand>
{
    private readonly IFamilyOsDbContext _db;

    public PatchDeadlineCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
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
