using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Deadlines;

public sealed class DeleteDeadlineCommandHandler : IRequestHandler<DeleteDeadlineCommand>
{
    private readonly IFamilyOsDbContext _db;

    public DeleteDeadlineCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteDeadlineCommand request, CancellationToken cancellationToken)
    {
        var deadline = await _db.Deadlines
            .FirstOrDefaultAsync(d => d.Id == request.DeadlineId, cancellationToken)
            ?? throw new NotFoundException("Deadline", request.DeadlineId);

        if (deadline.IsPrivate && deadline.CreatedByUserAccountId != request.UserId)
            throw new ForbiddenException("Nincs jogosultsága törölni ezt a határidőt.");

        deadline.SoftDelete();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
