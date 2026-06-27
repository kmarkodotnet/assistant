using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Domain.Services;

namespace FamilyOs.Domain.Tests.Services;

public sealed class DeadlineStateMachineTests
{
    private static Deadline CreateUpcomingDeadline() => Deadline.Create(
        "Test deadline",
        dueDateUtc: DateTime.UtcNow.AddDays(30),
        createdByUserAccountId: Guid.NewGuid());

    [Fact]
    public void Transition_UpcomingToDue_Succeeds()
    {
        var deadline = CreateUpcomingDeadline();
        DeadlineStateMachine.Transition(deadline, DeadlineStatus.Due);
        Assert.Equal(DeadlineStatus.Due, deadline.Status);
    }

    [Fact]
    public void Transition_UpcomingToResolved_Succeeds()
    {
        var deadline = CreateUpcomingDeadline();
        DeadlineStateMachine.Transition(deadline, DeadlineStatus.Resolved);
        Assert.Equal(DeadlineStatus.Resolved, deadline.Status);
    }

    [Fact]
    public void Transition_UpcomingToDismissed_Succeeds()
    {
        var deadline = CreateUpcomingDeadline();
        DeadlineStateMachine.Transition(deadline, DeadlineStatus.Dismissed);
        Assert.Equal(DeadlineStatus.Dismissed, deadline.Status);
    }

    [Fact]
    public void Transition_ResolvedToAnyOther_Throws()
    {
        var deadline = CreateUpcomingDeadline();
        DeadlineStateMachine.Transition(deadline, DeadlineStatus.Resolved);

        Assert.Throws<InvalidOperationException>(() =>
            DeadlineStateMachine.Transition(deadline, DeadlineStatus.Dismissed));
    }

    [Fact]
    public void Approve_SetsOriginAiApproved()
    {
        var userId = Guid.NewGuid();
        var deadline = CreateUpcomingDeadline();
        deadline.Approve(userId);

        Assert.Equal(userId, deadline.ApprovedByUserAccountId);
        Assert.NotNull(deadline.ApprovedUtc);
        Assert.Equal(Origin.AiApproved, deadline.Origin);
    }
}
