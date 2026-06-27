using FamilyOs.Application.Documents.Suggestions;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FluentAssertions;

namespace FamilyOs.Infrastructure.Tests.Pipeline;

public sealed class SuggestionDedupServiceTests
{
    [Fact]
    public void IsDeadlineDuplicate_SameTitleAndDate_ReturnsTrue()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var due = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var existing = new List<Deadline>
        {
            Deadline.CreateSuggestion("Biztosítás megújítás", due, docId, userId),
        };

        // Act
        var isDuplicate = SuggestionDedupService.IsDeadlineDuplicate(existing, "Biztosítás megújítás", due);

        // Assert
        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public void IsDeadlineDuplicate_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var due = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var existing = new List<Deadline>
        {
            Deadline.CreateSuggestion("biztosítás megújítás", due, docId, userId),
        };

        // Act
        var isDuplicate = SuggestionDedupService.IsDeadlineDuplicate(existing, "BIZTOSÍTÁS MEGÚJÍTÁS", due);

        // Assert
        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public void IsDeadlineDuplicate_DifferentTitle_ReturnsFalse()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var due = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var existing = new List<Deadline>
        {
            Deadline.CreateSuggestion("Biztosítás megújítás", due, docId, userId),
        };

        // Act
        var isDuplicate = SuggestionDedupService.IsDeadlineDuplicate(existing, "Más határidő", due);

        // Assert
        isDuplicate.Should().BeFalse();
    }

    [Fact]
    public void IsDeadlineDuplicate_DifferentDate_ReturnsFalse()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var due = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var differentDue = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var existing = new List<Deadline>
        {
            Deadline.CreateSuggestion("Biztosítás megújítás", due, docId, userId),
        };

        // Act
        var isDuplicate = SuggestionDedupService.IsDeadlineDuplicate(existing, "Biztosítás megújítás", differentDue);

        // Assert
        isDuplicate.Should().BeFalse();
    }

    [Fact]
    public void IsTaskDuplicate_SameTitle_ReturnsTrue()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var existing = new List<FamilyTask>
        {
            FamilyTask.CreateSuggestion("Ellenőrizni a számlát", docId, userId),
        };

        // Act
        var isDuplicate = SuggestionDedupService.IsTaskDuplicate(existing, "Ellenőrizni a számlát");

        // Assert
        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public void IsTaskDuplicate_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var existing = new List<FamilyTask>
        {
            FamilyTask.CreateSuggestion("ellenőrizni a számlát", docId, userId),
        };

        // Act
        var isDuplicate = SuggestionDedupService.IsTaskDuplicate(existing, "ELLENŐRIZNI A SZÁMLÁT");

        // Assert
        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public void IsTaskDuplicate_DifferentTitle_ReturnsFalse()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var existing = new List<FamilyTask>
        {
            FamilyTask.CreateSuggestion("Ellenőrizni a számlát", docId, userId),
        };

        // Act
        var isDuplicate = SuggestionDedupService.IsTaskDuplicate(existing, "Más feladat");

        // Assert
        isDuplicate.Should().BeFalse();
    }

    [Fact]
    public void IsDeadlineDuplicate_EmptyList_ReturnsFalse()
    {
        // Arrange
        var existing = new List<Deadline>();
        var due = DateTime.UtcNow.AddMonths(1);

        // Act
        var isDuplicate = SuggestionDedupService.IsDeadlineDuplicate(existing, "Test deadline", due);

        // Assert
        isDuplicate.Should().BeFalse();
    }
}
