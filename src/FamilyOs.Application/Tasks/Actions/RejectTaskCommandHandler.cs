using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Tasks.Actions;

public sealed class RejectTaskCommandHandler : IRequestHandler<RejectTaskCommand>
{
    private readonly IFamilyOsDbContext _db;

    public RejectTaskCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(RejectTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        if (task.Status != Domain.Enums.TaskStatus.Suggested)
            throw new DomainBusinessRuleException(
                $"Csak Suggested állapotú feladatot lehet elutasítani. Jelenlegi állapot: {task.Status}.");

        task.Reject();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
