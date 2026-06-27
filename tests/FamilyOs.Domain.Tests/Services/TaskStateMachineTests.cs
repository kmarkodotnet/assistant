using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Services;
using DomainTaskStatus = FamilyOs.Domain.Enums.TaskStatus;

namespace FamilyOs.Domain.Tests.Services;

public sealed class TaskStateMachineTests
{
    private static FamilyTask CreateOpenTask() => FamilyTask.Create(
        "Test task",
        createdByUserAccountId: Guid.NewGuid());

    [Fact]
    public void Transition_OpenToInProgress_Succeeds()
    {
        var task = CreateOpenTask();
        Assert.Equal(DomainTaskStatus.Open, task.Status);

        TaskStateMachine.Transition(task, DomainTaskStatus.InProgress);
        Assert.Equal(DomainTaskStatus.InProgress, task.Status);
    }

    [Fact]
    public void Transition_InProgressToDone_Succeeds()
    {
        var task = CreateOpenTask();
        TaskStateMachine.Transition(task, DomainTaskStatus.InProgress);
        TaskStateMachine.Transition(task, DomainTaskStatus.Done);
        Assert.Equal(DomainTaskStatus.Done, task.Status);
    }

    [Fact]
    public void Transition_DoneToAnyOther_Throws()
    {
        var task = CreateOpenTask();
        TaskStateMachine.Transition(task, DomainTaskStatus.InProgress);
        TaskStateMachine.Transition(task, DomainTaskStatus.Done);

        Assert.Throws<InvalidOperationException>(() =>
            TaskStateMachine.Transition(task, DomainTaskStatus.Open));
    }

    [Fact]
    public void Transition_CancelledToAnyOther_Throws()
    {
        var task = CreateOpenTask();
        TaskStateMachine.Transition(task, DomainTaskStatus.Cancelled);

        Assert.Throws<InvalidOperationException>(() =>
            TaskStateMachine.Transition(task, DomainTaskStatus.Open));
    }

    [Fact]
    public void Approve_SetsApprovalFieldsAndOrigin()
    {
        var userId = Guid.NewGuid();
        var task = FamilyTask.CreateSuggestion("Test", Guid.NewGuid(), userId);
        TaskStateMachine.Transition(task, DomainTaskStatus.Open);
        task.Approve(userId);

        Assert.Equal(userId, task.ApprovedByUserAccountId);
        Assert.NotNull(task.ApprovedUtc);
        Assert.Equal(Enums.Origin.AiApproved, task.Origin);
    }
}
