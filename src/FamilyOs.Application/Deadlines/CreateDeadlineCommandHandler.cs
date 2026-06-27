using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Deadlines.Dtos;
using FamilyOs.Domain.Entities;
using MediatR;

namespace FamilyOs.Application.Deadlines;

public sealed class CreateDeadlineCommandHandler : IRequestHandler<CreateDeadlineCommand, DeadlineDto>
{
    private readonly IFamilyOsDbContext _db;

    public CreateDeadlineCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<DeadlineDto> Handle(CreateDeadlineCommand request, CancellationToken cancellationToken)
    {
        var deadline = Deadline.Create(
            title: request.Title,
            dueDateUtc: request.DueDateUtc,
            createdByUserAccountId: request.CreatedByUserAccountId,
            description: request.Description,
            category: request.Category,
            relatedFamilyMemberId: request.RelatedFamilyMemberId,
            isPrivate: request.IsPrivate);

        _db.Deadlines.Add(deadline);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(deadline);
    }

    internal static DeadlineDto MapToDto(Deadline d) => new()
    {
        Id = d.Id,
        Title = d.Title,
        Description = d.Description,
        DueDateUtc = d.DueDateUtc,
        Status = d.Status.ToString(),
        Category = d.Category.ToString(),
        Origin = d.Origin.ToString(),
        SourceDocumentId = d.SourceDocumentId,
        RelatedFamilyMemberId = d.RelatedFamilyMemberId,
        CreatedByUserAccountId = d.CreatedByUserAccountId,
        IsPrivate = d.IsPrivate,
        CreatedUtc = d.CreatedUtc,
        UpdatedUtc = d.UpdatedUtc,
        ApprovedByUserAccountId = d.ApprovedByUserAccountId,
        ApprovedUtc = d.ApprovedUtc,
    };
}
