using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = FamilyOs.Domain.Enums.TaskStatus;

namespace FamilyOs.Application.Tasks.Actions;

public sealed class CompleteTaskCommandHandler : IRequestHandler<CompleteTaskCommand>
{
    private readonly IFamilyOsDbContext _db;

    public CompleteTaskCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        try
        {
            TaskStateMachine.Transition(task, DomainTaskStatus.Done);
        }
        catch (InvalidOperationException ex)
        {
            throw new DomainBusinessRuleException(ex.Message);
        }

        task.SetCompletedUtc();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
