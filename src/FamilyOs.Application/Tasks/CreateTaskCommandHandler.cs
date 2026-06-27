using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Tasks.Dtos;
using FamilyOs.Domain.Entities;
using MediatR;

namespace FamilyOs.Application.Tasks;

public sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IFamilyOsDbContext _db;

    public CreateTaskCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = FamilyTask.Create(
            title: request.Title,
            createdByUserAccountId: request.CreatedByUserAccountId,
            description: request.Description,
            dueDateUtc: request.DueDateUtc,
            priority: request.Priority,
            assignedToFamilyMemberId: request.AssignedToFamilyMemberId,
            isPrivate: request.IsPrivate);

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(task);
    }

    internal static TaskDto MapToDto(FamilyTask t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        DueDateUtc = t.DueDateUtc,
        Status = t.Status.ToString(),
        Priority = t.Priority.ToString(),
        Origin = t.Origin.ToString(),
        SourceDocumentId = t.SourceDocumentId,
        AssignedToFamilyMemberId = t.AssignedToFamilyMemberId,
        CreatedByUserAccountId = t.CreatedByUserAccountId,
        IsPrivate = t.IsPrivate,
        CreatedUtc = t.CreatedUtc,
        UpdatedUtc = t.UpdatedUtc,
        ApprovedByUserAccountId = t.ApprovedByUserAccountId,
        ApprovedUtc = t.ApprovedUtc,
        CompletedUtc = t.CompletedUtc,
    };
}
