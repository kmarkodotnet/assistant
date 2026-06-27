using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notes.Linking;

public sealed class RemoveNoteTopicCommandHandler : IRequestHandler<RemoveNoteTopicCommand>
{
    private readonly IFamilyOsDbContext _db;

    public RemoveNoteTopicCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(RemoveNoteTopicCommand request, CancellationToken cancellationToken)
    {
        var note = await _db.Notes
            .FirstOrDefaultAsync(n => n.Id == request.NoteId, cancellationToken)
            ?? throw new NotFoundException("Note", request.NoteId);

        if (note.CreatedByUserAccountId != request.RequestingUserId)
            throw new ForbiddenException("Csak a saját jegyzetét módosíthatja.");

        var noteTopic = await _db.NoteTopics
            .FirstOrDefaultAsync(nt => nt.NoteId == request.NoteId && nt.TopicId == request.TopicId, cancellationToken);

        if (noteTopic is not null)
        {
            _db.NoteTopics.Remove(noteTopic);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
