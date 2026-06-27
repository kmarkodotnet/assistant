using FamilyOs.Application.Notes.Dtos;
using MediatR;

namespace FamilyOs.Application.Notes;

public sealed record GetNoteQuery(Guid Id, Guid RequestingUserId) : IRequest<NoteDto>;
