using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Deadlines.Dtos;
using FamilyOs.Application.Tasks.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Suggestions;

public sealed class GetSuggestionsQueryHandler : IRequestHandler<GetSuggestionsQuery, SuggestionsAggregateDto>
{
    private readonly IFamilyOsDbContext _db;

    public GetSuggestionsQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<SuggestionsAggregateDto> Handle(
        GetSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        var suggestedTasks = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.Status == Domain.Enums.TaskStatus.Suggested)
            .Where(t => !t.IsPrivate || t.CreatedByUserAccountId == request.UserId)
            .OrderByDescending(t => t.CreatedUtc)
            .Take(50)
            .Select(t => new TaskListItemDto
            {
                Id = t.Id,
                Title = t.Title,
                DueDateUtc = t.DueDateUtc,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                Origin = t.Origin.ToString(),
                AssignedToFamilyMemberId = t.AssignedToFamilyMemberId,
                CreatedUtc = t.CreatedUtc,
            })
            .ToListAsync(cancellationToken);

        var suggestedDeadlines = await _db.Deadlines
            .AsNoTracking()
            .Where(d => d.Origin == Origin.AiSuggested)
            .Where(d => !d.IsPrivate || d.CreatedByUserAccountId == request.UserId)
            .OrderBy(d => d.DueDateUtc)
            .Take(50)
            .Select(d => new DeadlineListItemDto
            {
                Id = d.Id,
                Title = d.Title,
                DueDateUtc = d.DueDateUtc,
                Status = d.Status.ToString(),
                Category = d.Category.ToString(),
                Origin = d.Origin.ToString(),
                RelatedFamilyMemberId = d.RelatedFamilyMemberId,
                CreatedUtc = d.CreatedUtc,
            })
            .ToListAsync(cancellationToken);

        var pendingTags = await _db.DocumentTags
            .AsNoTracking()
            .Where(dt => !dt.IsApproved && dt.Origin == Origin.AiSuggested)
            .Join(_db.Tags, dt => dt.TagId, t => t.Id, (dt, t) => new DocumentTagSuggestionDto
            {
                DocumentId = dt.DocumentId,
                TagId = dt.TagId,
                TagName = t.Name,
            })
            .Take(50)
            .ToListAsync(cancellationToken);

        var pendingTopics = await _db.DocumentTopics
            .AsNoTracking()
            .Where(dt => !dt.IsApproved && dt.Origin == Origin.AiSuggested)
            .Join(_db.Topics, dt => dt.TopicId, t => t.Id, (dt, t) => new DocumentTopicSuggestionDto
            {
                DocumentId = dt.DocumentId,
                TopicId = dt.TopicId,
                TopicName = t.Name,
                TopicSlug = t.Slug,
            })
            .Take(50)
            .ToListAsync(cancellationToken);

        var total = suggestedTasks.Count + suggestedDeadlines.Count + pendingTags.Count + pendingTopics.Count;

        return new SuggestionsAggregateDto
        {
            Tasks = suggestedTasks,
            Deadlines = suggestedDeadlines,
            Tags = pendingTags,
            Topics = pendingTopics,
            TotalCount = total,
        };
    }
}
