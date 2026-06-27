using FamilyOs.Infrastructure.Ai.Prompts;
using FluentAssertions;

namespace FamilyOs.Infrastructure.Tests.Ai;

public sealed class PromptTemplateTests
{
    [Theory]
    [InlineData(PromptCatalog.SysPrefix)]
    [InlineData(PromptCatalog.Summarize)]
    [InlineData(PromptCatalog.Classify)]
    [InlineData(PromptCatalog.ExtractDeadlines)]
    [InlineData(PromptCatalog.ExtractTasks)]
    [InlineData(PromptCatalog.ExtractWarranty)]
    [InlineData(PromptCatalog.ExtractMedical)]
    [InlineData(PromptCatalog.ExtractFinancial)]
    public void Load_AllCatalogEntries_ReturnNonEmptyContent(string resourceName)
    {
        var content = PromptTemplate.Load(resourceName);

        content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Load_NonExistentResource_ThrowsInvalidOperationException()
    {
        var act = () => PromptTemplate.Load("does-not-exist.txt");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does-not-exist.txt*");
    }

    [Fact]
    public void Replace_ReplacesAllPlaceholders()
    {
        var template = "Hello {{NAME}}, your doc is {{TITLE}}.";
        var placeholders = new Dictionary<string, string>
        {
            ["NAME"] = "Alice",
            ["TITLE"] = "Invoice",
        };

        var result = PromptTemplate.Replace(template, placeholders);

        result.Should().Be("Hello Alice, your doc is Invoice.");
    }

    [Fact]
    public void Replace_UnknownPlaceholder_LeavesItUntouched()
    {
        var template = "Hello {{NAME}} and {{UNKNOWN}}.";
        var placeholders = new Dictionary<string, string> { ["NAME"] = "Alice" };

        var result = PromptTemplate.Replace(template, placeholders);

        result.Should().Be("Hello Alice and {{UNKNOWN}}.");
    }

    [Theory]
    [InlineData("sysprefix.v1.txt", "v1")]
    [InlineData("summarize.v1.txt", "v1")]
    [InlineData("extract-deadlines.v1.txt", "v1")]
    public void GetVersion_ReturnsCorrectVersion(string resourceName, string expectedVersion)
    {
        var version = PromptCatalog.GetVersion(resourceName);

        version.Should().Be(expectedVersion);
    }
}
