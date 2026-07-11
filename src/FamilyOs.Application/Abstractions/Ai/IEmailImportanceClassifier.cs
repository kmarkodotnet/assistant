using FamilyOs.Domain.Enums;

namespace FamilyOs.Application.Abstractions.Ai;

public record EmailImportanceResult(
    EmailImportance Importance,
    string? Category,
    bool HasDeadlineHint);

public interface IEmailImportanceClassifier
{
    Task<EmailImportanceResult> ClassifyAsync(string subject, string? bodyText, CancellationToken ct = default);
}
