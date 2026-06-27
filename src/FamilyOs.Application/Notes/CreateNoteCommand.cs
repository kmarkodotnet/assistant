using FamilyOs.Application.Notes.Dtos;
using MediatR;

namespace FamilyOs.Application.Notes;

public sealed record CreateNoteCommand(
    string Title,
    string Body,
    Guid CreatedByUserId,
    Guid? RelatedFamilyMemberId,
    bool IsPrivate) : IRequest<NoteDto>;
