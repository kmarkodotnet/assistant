using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Ai.Providers;
using FamilyOs.Infrastructure.Ai.Tasks;
using FamilyOs.Infrastructure.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FamilyOs.Infrastructure.Tests.Ai;

/// <summary>
/// Unit tests for OllamaEmailClassifier, focused on the defensive JSON-parse contract
/// (docs/contracts/classify-email-contract.md §2.5): a malformed / missing-field AI response must
/// never throw and must default to a safe Low/no-notification result.
/// </summary>
public sealed class OllamaEmailClassifierTests
{
    private static OllamaEmailClassifier BuildClassifier(string providerResponseJson)
    {
        var provider = new InMemoryAiProvider(new Dictionary<string, string>
        {
            ["classify-email.v1.txt"] = providerResponseJson,
        });

        var factory = Substitute.For<IAiProviderFactory>();
        factory.GetProvider().Returns(provider);

        return new OllamaEmailClassifier(factory, NullLogger<OllamaEmailClassifier>.Instance);
    }

    [Fact]
    public async Task ClassifyAsync_ValidJson_ParsesAllFields()
    {
        var classifier = BuildClassifier(
            """{"importance": "High", "category": "hivatalos", "hasDeadline": true}""");

        var result = await classifier.ClassifyAsync("Fizetési felszólítás", "A díj augusztus 5-én esedékes.");

        result.Importance.Should().Be(EmailImportance.High);
        result.Category.Should().Be("hivatalos");
        result.HasDeadlineHint.Should().BeTrue();
    }

    [Fact]
    public async Task ClassifyAsync_MalformedJson_DefaultsToLowWithoutThrowing()
    {
        var classifier = BuildClassifier("not valid json {{{");

        // The classifier must never propagate a JSON parse failure as an exception (contract §2.5).
        var result = await classifier.ClassifyAsync("Subject", "Body");

        result.Importance.Should().Be(EmailImportance.Low);
        result.Category.Should().BeNull();
        result.HasDeadlineHint.Should().BeFalse();
    }

    [Fact]
    public async Task ClassifyAsync_MissingImportanceField_DefaultsToLow()
    {
        var classifier = BuildClassifier("""{"category": "hirlevel"}""");

        var result = await classifier.ClassifyAsync("Subject", "Body");

        result.Importance.Should().Be(EmailImportance.Low);
    }

    [Fact]
    public async Task ClassifyAsync_UnknownImportanceValue_DefaultsToLow()
    {
        var classifier = BuildClassifier("""{"importance": "Urgentissimo", "category": "x", "hasDeadline": false}""");

        var result = await classifier.ClassifyAsync("Subject", "Body");

        result.Importance.Should().Be(EmailImportance.Low);
    }

    [Fact]
    public async Task ClassifyAsync_NullCategoryLiteral_MapsToNull()
    {
        var classifier = BuildClassifier("""{"importance": "Medium", "category": "null", "hasDeadline": false}""");

        var result = await classifier.ClassifyAsync("Subject", "Body");

        result.Category.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyAsync_EmptyBody_StillClassifiesFromSubjectOnly()
    {
        var classifier = BuildClassifier("""{"importance": "Medium", "category": "szemelyes", "hasDeadline": false}""");

        var result = await classifier.ClassifyAsync("Csak targy", null);

        result.Importance.Should().Be(EmailImportance.Medium);
    }
}
