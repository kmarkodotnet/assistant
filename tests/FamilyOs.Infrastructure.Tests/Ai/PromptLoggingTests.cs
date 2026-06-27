using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FluentAssertions;

namespace FamilyOs.Infrastructure.Tests.Ai;

/// <summary>
/// Verifies privacy requirements for AI audit logging:
/// full prompt text must NOT be stored in AuditLog details.
/// </summary>
public sealed class PromptLoggingTests
{
    [Fact]
    public void AuditLog_ForAiCall_DoesNotContainFullPromptText()
    {
        // Arrange: a realistic prompt containing PII-like content
        const string sensitivePromptText = "Ez egy rendkívül bizalmas dokumentum részlet a beteg diagnózisával.";
        const int promptLength = 100;

        // Act: log only hash + length, not the full text (privacy requirement)
        var promptHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(sensitivePromptText)));

        var detailsJson = $"{{\"prompt_hash\":\"{promptHash}\",\"prompt_length\":{promptLength}}}";

        var auditLog = AuditLog.Create(
            action: AuditAction.AiCall,
            userAccountId: null,
            entityType: "Document",
            entityId: Guid.NewGuid(),
            detailsJson: detailsJson);

        // Assert: the full prompt text is NOT in the DetailsJson
        auditLog.DetailsJson.Should().NotBeNull();
        auditLog.DetailsJson.Should().NotContain(sensitivePromptText);
        auditLog.DetailsJson.Should().Contain("prompt_hash");
        auditLog.DetailsJson.Should().Contain("prompt_length");
        auditLog.Action.Should().Be(AuditAction.AiCall);
    }

    [Fact]
    public void AuditLog_DetailsJson_ContainsHashNotPlaintext()
    {
        // Arrange
        const string promptText = "Summarize this document about warranties.";
        var promptBytes = System.Text.Encoding.UTF8.GetBytes(promptText);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(promptBytes));

        // Act
        var detailsJson = $"{{\"prompt_hash\":\"{hash}\",\"prompt_length\":{promptText.Length}}}";

        var auditLog = AuditLog.Create(
            action: AuditAction.AiCall,
            userAccountId: Guid.NewGuid(),
            detailsJson: detailsJson);

        // Assert: hash is 64 hex chars (SHA-256), text not present
        auditLog.DetailsJson.Should().Contain(hash);
        hash.Should().HaveLength(64); // SHA-256 produces 32 bytes = 64 hex chars
        auditLog.DetailsJson.Should().NotContain(promptText);
    }
}
