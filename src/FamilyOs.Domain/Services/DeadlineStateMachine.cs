using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Services;

public static class DeadlineStateMachine
{
    private static readonly Dictionary<DeadlineStatus, DeadlineStatus[]> AllowedTransitions = new()
    {
        [DeadlineStatus.Upcoming] = [DeadlineStatus.Due, DeadlineStatus.Resolved, DeadlineStatus.Dismissed],
        [DeadlineStatus.Due] = [DeadlineStatus.Passed, DeadlineStatus.Resolved, DeadlineStatus.Dismissed],
        [DeadlineStatus.Passed] = [DeadlineStatus.Resolved, DeadlineStatus.Dismissed],
        [DeadlineStatus.Resolved] = [],
        [DeadlineStatus.Dismissed] = [],
    };

    public static void Transition(Deadline deadline, DeadlineStatus newStatus)
    {
        if (!AllowedTransitions.TryGetValue(deadline.Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException(
                $"Érvénytelen állapotátmenet: {deadline.Status} → {newStatus}.");
        deadline.SetStatus(newStatus);
    }
}
