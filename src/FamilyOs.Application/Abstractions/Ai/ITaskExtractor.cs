namespace FamilyOs.Application.Abstractions.Ai;

public record TaskSuggestion(string Title, string? AssignedToHint, DateOnly? DueDate, string? Description);

public interface ITaskExtractor
{
    Task<IReadOnlyList<TaskSuggestion>> ExtractAsync(string documentText, IReadOnlyList<string> familyMemberNames, CancellationToken ct = default);
}
