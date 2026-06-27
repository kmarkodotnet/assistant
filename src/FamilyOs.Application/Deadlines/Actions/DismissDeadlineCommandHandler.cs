using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Enums;
using FamilyOs.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Deadlines.Actions;

public sealed class DismissDeadlineCommandHandler : IRequestHandler<DismissDeadlineCommand>
{
    private readonly IFamilyOsDbContext _db;

    public DismissDeadlineCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DismissDeadlineCommand request, CancellationToken cancellationToken)
    {
        var deadline = await _db.Deadlines
            .FirstOrDefaultAsync(d => d.Id == request.DeadlineId, cancellationToken)
            ?? throw new NotFoundException("Deadline", request.DeadlineId);

        try
        {
            DeadlineStateMachine.Transition(deadline, DeadlineStatus.Dismissed);
        }
        catch (InvalidOperationException ex)
        {
            throw new DomainBusinessRuleException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
