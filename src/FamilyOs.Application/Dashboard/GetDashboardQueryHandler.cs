using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Deadlines.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Dashboard;

public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IFamilyOsDbContext _db;

    public GetDashboardQueryHandler(IFamilyOsDbContext db) => _db = db;

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var in7Days = now.AddDays(7);

        // EF Core DbContext is not thread-safe — queries must run sequentially
        var upcoming = await _db.Deadlines
            .AsNoTracking()
            .Where(d => d.DueDateUtc >= now && d.DueDateUtc <= in7Days
                && d.Status == DeadlineStatus.Upcoming
                && (!d.IsPrivate || d.CreatedByUserAccountId == request.UserId))
            .OrderBy(d => d.DueDateUtc)
            .Take(10)
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

        var overdue = await _db.Deadlines
            .AsNoTracking()
            .Where(d => d.DueDateUtc < now
                && (d.Status == DeadlineStatus.Due || d.Status == DeadlineStatus.Passed)
                && (!d.IsPrivate || d.CreatedByUserAccountId == request.UserId))
            .OrderBy(d => d.DueDateUtc)
            .Take(10)
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

        var recentDocs = await _db.Documents
            .AsNoTracking()
            .Where(d => d.DeletedUtc == null
                && (!d.IsPrivate || d.CreatedByUserAccountId == request.UserId))
            .OrderByDescending(d => d.CreatedUtc)
            .Take(5)
            .Select(d => new RecentDocumentDto
            {
                Id = d.Id,
                Title = d.Title,
                OriginalFileName = d.OriginalFileName,
                MimeType = d.MimeType,
                CreatedUtc = d.CreatedUtc,
            })
            .ToListAsync(cancellationToken);

        var savedSearches = await _db.SavedSearches
            .AsNoTracking()
            .Where(s => s.UserAccountId == request.UserId)
            .OrderByDescending(s => s.CreatedUtc)
            .Take(5)
            .Select(s => new SavedSearchDto
            {
                Id = s.Id,
                Name = s.Name,
                QueryJson = s.QueryJson,
                CreatedUtc = s.CreatedUtc,
            })
            .ToListAsync(cancellationToken);

        var suggestions = await CountSuggestionsAsync(request.UserId, cancellationToken);

        return new DashboardDto
        {
            UpcomingDeadlines = upcoming,
            OverdueReminders = overdue,
            RecentDocuments = recentDocs,
            SavedSearches = savedSearches,
            PendingSuggestions = suggestions,
        };
    }

    private async Task<DashboardSuggestionsCountDto> CountSuggestionsAsync(Guid userId, CancellationToken ct)
    {
        var tasks = await _db.Tasks.CountAsync(t =>
            t.Status == Domain.Enums.TaskStatus.Suggested
            && (!t.IsPrivate || t.CreatedByUserAccountId == userId), ct);

        var deadlines = await _db.Deadlines.CountAsync(d =>
            d.Origin == Origin.AiSuggested
            && (!d.IsPrivate || d.CreatedByUserAccountId == userId), ct);

        var tags = await _db.DocumentTags.CountAsync(dt =>
            !dt.IsApproved && dt.Origin == Origin.AiSuggested, ct);

        var topics = await _db.DocumentTopics.CountAsync(dt =>
            !dt.IsApproved && dt.Origin == Origin.AiSuggested, ct);

        return new DashboardSuggestionsCountDto
        {
            Tasks = tasks,
            Deadlines = deadlines,
            Tags = tags,
            Topics = topics,
            Total = tasks + deadlines + tags + topics,
        };
    }
}
