using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notes.Linking;

public sealed class RemoveNoteTagCommandHandler : IRequestHandler<RemoveNoteTagCommand>
{
    private readonly IFamilyOsDbContext _db;

    public RemoveNoteTagCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(RemoveNoteTagCommand request, CancellationToken cancellationToken)
    {
        var note = await _db.Notes
            .FirstOrDefaultAsync(n => n.Id == request.NoteId, cancellationToken)
            ?? throw new NotFoundException("Note", request.NoteId);

        if (note.CreatedByUserAccountId != request.RequestingUserId)
            throw new ForbiddenException("Csak a saját jegyzetét módosíthatja.");

        var noteTag = await _db.NoteTags
            .FirstOrDefaultAsync(nt => nt.NoteId == request.NoteId && nt.TagId == request.TagId, cancellationToken);

        if (noteTag is not null)
        {
            _db.NoteTags.Remove(noteTag);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
