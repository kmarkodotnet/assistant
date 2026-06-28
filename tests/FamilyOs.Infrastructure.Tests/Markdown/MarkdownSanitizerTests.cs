using FamilyOs.Infrastructure.Markdown;

namespace FamilyOs.Infrastructure.Tests.Markdown;

public sealed class MarkdownSanitizerTests
{
    private readonly MarkdownSanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_RemovesScriptTags()
    {
        const string input = "Hello <script>alert('xss')</script> world";

        var result = _sanitizer.Sanitize(input);

        Assert.DoesNotContain("<script>", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesOnClickAttributes()
    {
        const string input = "<a href=\"/\" onclick=\"evil()\">click</a>";

        var result = _sanitizer.Sanitize(input);

        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_ConvertsMarkdownToHtml()
    {
        const string input = "# Title\n\nSome **bold** text.";

        var result = _sanitizer.Sanitize(input);

        Assert.Contains("<h1", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<strong>", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_EmptyInput_ReturnsEmpty()
    {
        var result = _sanitizer.Sanitize(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_NullWhitespace_ReturnsEmpty()
    {
        var result = _sanitizer.Sanitize("   ");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_PreservesNormalLinks()
    {
        const string input = "[Click here](https://example.com)";

        var result = _sanitizer.Sanitize(input);

        Assert.Contains("href=\"https://example.com\"", result, StringComparison.OrdinalIgnoreCase);
    }
}
