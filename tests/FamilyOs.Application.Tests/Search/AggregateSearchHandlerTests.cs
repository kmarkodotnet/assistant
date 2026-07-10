using FamilyOs.Application.Search.Dtos;
using FamilyOs.Application.Search.Handlers;
using FamilyOs.Application.Tests.Common;

namespace FamilyOs.Application.Tests.Search;

public sealed class AggregateSearchHandlerTests
{
    [Fact]
    public void ResolveDateRange_NoRequestDates_DefaultsToLast6MonthsFromToday()
    {
        var clock = new FakeClock(); // Today = 2026-06-26
        var req = new SearchRequest { Query = "mennyi villanyszámlát fizettünk" };

        var (from, to) = AggregateSearchHandler.ResolveDateRange(req, clock.Today);

        Assert.Equal(new DateOnly(2025, 12, 26), from);
        Assert.Equal(new DateOnly(2026, 6, 26), to);
    }

    [Fact]
    public void ResolveDateRange_RequestDatesProvided_UsesThemVerbatim()
    {
        var clock = new FakeClock();
        var req = new SearchRequest
        {
            Query = "mennyi villanyszámlát fizettünk",
            From = new DateOnly(2026, 1, 1),
            To = new DateOnly(2026, 3, 31),
        };

        var (from, to) = AggregateSearchHandler.ResolveDateRange(req, clock.Today);

        Assert.Equal(new DateOnly(2026, 1, 1), from);
        Assert.Equal(new DateOnly(2026, 3, 31), to);
    }

    [Theory]
    [InlineData("Mennyi villanyszámlát fizettünk az elmúlt 6 hónapban?", "villan")]
    [InlineData("Összesen mennyit fizettünk a biztosításra?", "biztos")]
    [InlineData("Mennyi fizettünk?", null)]
    public void ExtractVendorHint_ReturnsExpectedHint(string query, string? expected)
    {
        var hint = AggregateSearchHandler.ExtractVendorHint(query);

        Assert.Equal(expected, hint);
    }

    [Fact]
    public void FormatAnswer_WithoutVendorHint_ProducesSummarySentence()
    {
        var range = (new DateOnly(2025, 12, 26), new DateOnly(2026, 6, 26));

        var answer = AggregateSearchHandler.FormatAnswer(123_456m, "HUF", 4, range, null);

        Assert.Contains("123,456 HUF", answer);
        Assert.Contains("4 tétel alapján", answer);
        Assert.Contains("2025.12.26", answer);
        Assert.Contains("2026.06.26", answer);
        Assert.DoesNotContain("(a(z)", answer);
    }

    [Fact]
    public void FormatAnswer_WithVendorHint_MentionsHintInParentheses()
    {
        var range = (new DateOnly(2025, 12, 26), new DateOnly(2026, 6, 26));

        var answer = AggregateSearchHandler.FormatAnswer(10_000m, "HUF", 1, range, "villany");

        Assert.Contains("(a(z) \"villany\" tételekre)", answer);
    }
}
