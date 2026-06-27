using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notes;

public sealed class DeleteNoteCommandHandler : IRequestHandler<DeleteNoteCommand>
{
    private readonly IFamilyOsDbContext _db;

    public DeleteNoteCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _db.Notes
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Note", request.Id);

        if (note.CreatedByUserAccountId != request.RequestingUserId)
            throw new ForbiddenException("Csak a saját jegyzetét törölheti.");

        note.SoftDelete();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
