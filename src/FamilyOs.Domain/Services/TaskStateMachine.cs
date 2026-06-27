using FamilyOs.Domain.Entities;
using DomainTaskStatus = FamilyOs.Domain.Enums.TaskStatus;

namespace FamilyOs.Domain.Services;

public static class TaskStateMachine
{
    private static readonly Dictionary<DomainTaskStatus, DomainTaskStatus[]> AllowedTransitions = new()
    {
        [DomainTaskStatus.Suggested] = [DomainTaskStatus.Open],
        [DomainTaskStatus.Open] = [DomainTaskStatus.InProgress, DomainTaskStatus.Done, DomainTaskStatus.Cancelled],
        [DomainTaskStatus.InProgress] = [DomainTaskStatus.Done, DomainTaskStatus.Cancelled],
        [DomainTaskStatus.Done] = [],
        [DomainTaskStatus.Cancelled] = [],
    };

    public static void Transition(FamilyTask task, DomainTaskStatus newStatus)
    {
        if (!AllowedTransitions.TryGetValue(task.Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException(
                $"Érvénytelen állapotátmenet: {task.Status} → {newStatus}.");
        task.SetStatus(newStatus);
    }
}
