using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Application.Notes.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notes;

public sealed class GetNoteQueryHandler : IRequestHandler<GetNoteQuery, NoteDto>
{
    private readonly IFamilyOsDbContext _db;

    public GetNoteQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<NoteDto> Handle(GetNoteQuery request, CancellationToken cancellationToken)
    {
        var note = await _db.Notes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Note", request.Id);

        if (note.IsPrivate && note.CreatedByUserAccountId != request.RequestingUserId)
            throw new ForbiddenException("Ehhez a jegyzethez nincs hozzáférési joga.");

        return CreateNoteCommandHandler.MapToDto(note);
    }
}
