using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = FamilyOs.Domain.Enums.TaskStatus;

namespace FamilyOs.Application.Tasks.Actions;

public sealed class StartTaskCommandHandler : IRequestHandler<StartTaskCommand>
{
    private readonly IFamilyOsDbContext _db;

    public StartTaskCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(StartTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        try
        {
            TaskStateMachine.Transition(task, DomainTaskStatus.InProgress);
        }
        catch (InvalidOperationException ex)
        {
            throw new DomainBusinessRuleException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
