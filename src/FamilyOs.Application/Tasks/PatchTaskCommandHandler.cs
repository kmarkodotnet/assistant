using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Tasks;

public sealed class PatchTaskCommandHandler : IRequestHandler<PatchTaskCommand>
{
    private readonly IFamilyOsDbContext _db;

    public PatchTaskCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(PatchTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        if (task.IsPrivate && task.CreatedByUserAccountId != request.UserId)
            throw new ForbiddenException("Nincs jogosultsága szerkeszteni ezt a feladatot.");

        task.UpdateDetails(
            request.Title,
            request.Description,
            request.DueDateUtc,
            request.Priority,
            request.AssignedToFamilyMemberId,
            request.IsPrivate);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("A feladatot közben módosították, töltse be újra.");
        }
    }
}
