namespace FamilyOs.Application.Abstractions.Ai;

public record DeadlineSuggestion(string Title, DateOnly DueDate, string? Description);

public interface IDeadlineExtractor
{
    Task<IReadOnlyList<DeadlineSuggestion>> ExtractAsync(string documentText, DateOnly today, CancellationToken ct = default);
}
