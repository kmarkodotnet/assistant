using FamilyOs.Application.Notes.Dtos;
using MediatR;

namespace FamilyOs.Application.Notes;

public sealed record ListNotesQuery(
    Guid RequestingUserId,
    Guid? RelatedFamilyMemberId,
    Guid? TagId,
    string? TopicSlug,
    bool IncludeBody,
    int Page,
    int PageSize) : IRequest<List<NoteListItemDto>>;
