using FamilyOs.Application.Deadlines.Dtos;
using FamilyOs.Application.Tasks.Dtos;

namespace FamilyOs.Application.Dashboard;

public sealed class DashboardDto
{
    public List<DeadlineListItemDto> UpcomingDeadlines { get; set; } = [];
    public List<DeadlineListItemDto> OverdueReminders { get; set; } = [];
    public DashboardSuggestionsCountDto PendingSuggestions { get; set; } = new();
    public List<RecentDocumentDto> RecentDocuments { get; set; } = [];
    public List<SavedSearchDto> SavedSearches { get; set; } = [];
}

public sealed class DashboardSuggestionsCountDto
{
    public int Tasks { get; set; }
    public int Deadlines { get; set; }
    public int Tags { get; set; }
    public int Topics { get; set; }
    public int Total { get; set; }
}

public sealed class RecentDocumentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public sealed class SavedSearchDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string QueryJson { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
