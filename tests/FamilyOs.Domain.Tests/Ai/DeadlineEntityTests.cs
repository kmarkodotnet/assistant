using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FluentAssertions;

namespace FamilyOs.Domain.Tests.Ai;

public sealed class DeadlineEntityTests
{
    [Fact]
    public void CreateSuggestion_SetsCorrectDefaults()
    {
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var due = DateTime.UtcNow.AddMonths(3);

        var deadline = Deadline.CreateSuggestion("Insurance renewal", due, docId, userId);

        deadline.Id.Should().NotBe(Guid.Empty);
        deadline.Title.Should().Be("Insurance renewal");
        deadline.DueDateUtc.Should().Be(due);
        deadline.Status.Should().Be(DeadlineStatus.Upcoming);
        deadline.Origin.Should().Be(Origin.AiSuggested);
        deadline.SourceDocumentId.Should().Be(docId);
        deadline.CreatedByUserAccountId.Should().Be(userId);
        deadline.IsPrivate.Should().BeFalse();
        deadline.DeletedUtc.Should().BeNull();
        deadline.Category.Should().Be(DeadlineCategory.Other);
    }

    [Fact]
    public void CreateSuggestion_WithCategory_SetsCategory()
    {
        var deadline = Deadline.CreateSuggestion(
            "Medical appointment",
            DateTime.UtcNow.AddMonths(1),
            Guid.NewGuid(),
            Guid.NewGuid(),
            category: DeadlineCategory.Medical);

        deadline.Category.Should().Be(DeadlineCategory.Medical);
    }

    [Fact]
    public void CreateSuggestion_WithDescription_SetsDescription()
    {
        var deadline = Deadline.CreateSuggestion(
            "Test",
            DateTime.UtcNow.AddMonths(1),
            Guid.NewGuid(),
            Guid.NewGuid(),
            description: "Important deadline");

        deadline.Description.Should().Be("Important deadline");
    }
}
