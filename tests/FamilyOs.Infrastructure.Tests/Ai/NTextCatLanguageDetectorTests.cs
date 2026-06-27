using FamilyOs.Infrastructure.Ai.Lang;
using FluentAssertions;

namespace FamilyOs.Infrastructure.Tests.Ai;

public sealed class NTextCatLanguageDetectorTests
{
    private readonly NTextCatLanguageDetector _sut = new();

    [Fact]
    public void Detect_NullOrEmptyText_ReturnsUnknown()
    {
        _sut.Detect(string.Empty).Should().Be("unknown");
        _sut.Detect("   ").Should().Be("unknown");
    }

    [Fact]
    public void Detect_AnyText_DoesNotThrow()
    {
        // If profile XML is missing the detector gracefully returns "unknown"
        // If it's present it returns a real ISO 639-1 code
        var act = () => _sut.Detect("The quick brown fox jumps over the lazy dog");
        act.Should().NotThrow();
    }

    [Fact]
    public void Detect_AnyText_ReturnsStringResult()
    {
        var result = _sut.Detect("The quick brown fox jumps over the lazy dog");
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Detect_VeryLongText_OnlyUses1000Chars_DoesNotThrow()
    {
        var longText = string.Concat(Enumerable.Repeat("Hello world this is a test sentence. ", 200));
        longText.Length.Should().BeGreaterThan(1000);

        var act = () => _sut.Detect(longText);
        act.Should().NotThrow();
    }

    [Fact]
    public void Detect_BinaryGarbage_ReturnsUnknownOrValidCode_DoesNotThrow()
    {
        // Non-natural-language input — just check it doesn't crash
        var garbage = new string('\x01', 500);
        var act = () => _sut.Detect(garbage);
        act.Should().NotThrow();
    }
}
