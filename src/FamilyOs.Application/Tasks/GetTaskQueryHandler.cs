using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Application.Tasks.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Tasks;

public sealed class GetTaskQueryHandler : IRequestHandler<GetTaskQuery, TaskDto>
{
    private readonly IFamilyOsDbContext _db;

    public GetTaskQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<TaskDto> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        if (task.IsPrivate && task.CreatedByUserAccountId != request.UserId)
            throw new ForbiddenException("Nincs jogosultsága megtekinteni ezt a feladatot.");

        return CreateTaskCommandHandler.MapToDto(task);
    }
}
