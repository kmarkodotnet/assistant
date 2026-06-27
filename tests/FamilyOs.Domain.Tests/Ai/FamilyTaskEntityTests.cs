using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FluentAssertions;

namespace FamilyOs.Domain.Tests.Ai;

public sealed class FamilyTaskEntityTests
{
    [Fact]
    public void CreateSuggestion_SetsCorrectDefaults()
    {
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var task = FamilyTask.CreateSuggestion("Check invoice", docId, userId);

        task.Id.Should().NotBe(Guid.Empty);
        task.Title.Should().Be("Check invoice");
        task.Status.Should().Be(Domain.Enums.TaskStatus.Suggested);
        task.Priority.Should().Be(Priority.Normal);
        task.Origin.Should().Be(Origin.AiSuggested);
        task.SourceDocumentId.Should().Be(docId);
        task.CreatedByUserAccountId.Should().Be(userId);
        task.IsPrivate.Should().BeFalse();
        task.DeletedUtc.Should().BeNull();
        task.DueDateUtc.Should().BeNull();
        task.AssignedToFamilyMemberId.Should().BeNull();
    }

    [Fact]
    public void CreateSuggestion_WithAssignment_SetsAssignment()
    {
        var memberId = Guid.NewGuid();
        var task = FamilyTask.CreateSuggestion(
            "Pay bill",
            Guid.NewGuid(),
            Guid.NewGuid(),
            assignedToFamilyMemberId: memberId);

        task.AssignedToFamilyMemberId.Should().Be(memberId);
    }

    [Fact]
    public void CreateSuggestion_WithDueDate_SetsDueDate()
    {
        var due = DateTime.UtcNow.AddDays(7);
        var task = FamilyTask.CreateSuggestion(
            "Submit form",
            Guid.NewGuid(),
            Guid.NewGuid(),
            dueDateUtc: due);

        task.DueDateUtc.Should().Be(due);
    }
}
