using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Deadlines.Actions;

public sealed class ApproveDeadlineCommandHandler : IRequestHandler<ApproveDeadlineCommand>
{
    private readonly IFamilyOsDbContext _db;

    public ApproveDeadlineCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(ApproveDeadlineCommand request, CancellationToken cancellationToken)
    {
        var deadline = await _db.Deadlines
            .FirstOrDefaultAsync(d => d.Id == request.DeadlineId, cancellationToken)
            ?? throw new NotFoundException("Deadline", request.DeadlineId);

        // Approve: set Origin=AiApproved and record who approved
        deadline.Approve(request.ApprovedByUserId);

        await _db.SaveChangesAsync(cancellationToken);

        // TODO (Epic G): Create Reminder entities based on DefaultReminderPolicy
        // The Reminder entity doesn't exist yet; skip reminder creation for now.
    }
}
