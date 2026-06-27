using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notes.Linking;

public sealed class AddNoteTopicCommandHandler : IRequestHandler<AddNoteTopicCommand>
{
    private readonly IFamilyOsDbContext _db;

    public AddNoteTopicCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AddNoteTopicCommand request, CancellationToken cancellationToken)
    {
        var note = await _db.Notes
            .FirstOrDefaultAsync(n => n.Id == request.NoteId, cancellationToken)
            ?? throw new NotFoundException("Note", request.NoteId);

        if (note.CreatedByUserAccountId != request.RequestingUserId)
            throw new ForbiddenException("Csak a saját jegyzetét módosíthatja.");

        _ = await _db.Topics
            .FirstOrDefaultAsync(t => t.Id == request.TopicId, cancellationToken)
            ?? throw new NotFoundException("Topic", request.TopicId);

        var existing = await _db.NoteTopics
            .FirstOrDefaultAsync(nt => nt.NoteId == request.NoteId && nt.TopicId == request.TopicId, cancellationToken);

        if (existing is null)
        {
            _db.NoteTopics.Add(new NoteTopic
            {
                NoteId = request.NoteId,
                TopicId = request.TopicId,
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
