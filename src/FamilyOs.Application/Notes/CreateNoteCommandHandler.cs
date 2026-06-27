using FamilyOs.Application.Common.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Notes.Dtos;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Notes;

public sealed class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, NoteDto>
{
    private readonly IFamilyOsDbContext _db;
    private readonly IAiProcessingJobRepository _jobRepository;

    public CreateNoteCommandHandler(IFamilyOsDbContext db, IAiProcessingJobRepository jobRepository)
    {
        _db = db;
        _jobRepository = jobRepository;
    }

    public async Task<NoteDto> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
        var note = Note.Create(
            title: request.Title,
            body: request.Body,
            createdByUserId: request.CreatedByUserId,
            relatedFamilyMemberId: request.RelatedFamilyMemberId,
            isPrivate: request.IsPrivate);

        _db.Notes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);

        // Enqueue embedding job
        var job = AiProcessingJob.CreateForNote(AiJobType.Embed, note.Id);
        await _jobRepository.AddAsync(job, cancellationToken);

        return MapToDto(note);
    }

    internal static NoteDto MapToDto(Note n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Body = n.Body,
        RelatedFamilyMemberId = n.RelatedFamilyMemberId,
        CreatedByUserAccountId = n.CreatedByUserAccountId,
        IsPrivate = n.IsPrivate,
        CreatedUtc = n.CreatedUtc,
        UpdatedUtc = n.UpdatedUtc,
    };
}
