using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notes.Linking;

public sealed class AddNoteTagCommandHandler : IRequestHandler<AddNoteTagCommand>
{
    private readonly IFamilyOsDbContext _db;

    public AddNoteTagCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AddNoteTagCommand request, CancellationToken cancellationToken)
    {
        var note = await _db.Notes
            .FirstOrDefaultAsync(n => n.Id == request.NoteId, cancellationToken)
            ?? throw new NotFoundException("Note", request.NoteId);

        if (note.CreatedByUserAccountId != request.RequestingUserId)
            throw new ForbiddenException("Csak a saját jegyzetét módosíthatja.");

        var tag = await _db.Tags
            .FirstOrDefaultAsync(t => t.Id == request.TagId, cancellationToken)
            ?? throw new NotFoundException("Tag", request.TagId);

        var existing = await _db.NoteTags
            .FirstOrDefaultAsync(nt => nt.NoteId == request.NoteId && nt.TagId == request.TagId, cancellationToken);

        if (existing is null)
        {
            _db.NoteTags.Add(new NoteTag
            {
                NoteId = request.NoteId,
                TagId = request.TagId,
                Origin = Origin.Manual,
                IsApproved = true,
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
