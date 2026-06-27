using FamilyOs.Domain.Entities;

namespace FamilyOs.Application.Documents.Suggestions;

public static class SuggestionDedupService
{
    public static bool IsDeadlineDuplicate(IReadOnlyList<Deadline> existing, string title, DateTime dueDate)
        => existing.Any(d =>
            string.Equals(d.Title, title, StringComparison.OrdinalIgnoreCase)
            && d.DueDateUtc.Date == dueDate.Date);

    public static bool IsTaskDuplicate(IReadOnlyList<FamilyTask> existing, string title)
        => existing.Any(t =>
            string.Equals(t.Title, title, StringComparison.OrdinalIgnoreCase));
}
