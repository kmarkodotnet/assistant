using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Tasks;

public sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand>
{
    private readonly IFamilyOsDbContext _db;

    public DeleteTaskCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        if (task.IsPrivate && task.CreatedByUserAccountId != request.UserId)
            throw new ForbiddenException("Nincs jogosultsága törölni ezt a feladatot.");

        task.SoftDelete();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
