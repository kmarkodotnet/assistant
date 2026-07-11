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
    // Regression: "kiadásom" (inflected "kiadás") must NOT be read as a vendor-name hint —
    // it's a generic aggregation word, not a document/vendor keyword. Bug found via manual
    // testing: real FinancialRecord data existed but "mennyi kiadásom volt összesen" answered
    // "Nincs erre vonatkozó adat", because the exact-word stopword list didn't cover inflected
    // forms of "kiadás", so it was mistaken for a vendor filter.
    [InlineData("Mennyi kiadásom volt összesen?", null)]
    [InlineData("Mennyi volt a bevételünk?", null)]
    public void ExtractVendorHint_ReturnsExpectedHint(string query, string? expected)
    {
        var hint = AggregateSearchHandler.ExtractVendorHint(query);

        Assert.Equal(expected, hint);
    }

    [Fact]
    public void FormatAnswer_WithoutVendorHint_ProducesSummarySentence()
    {
        var range = (new DateOnly(2025, 12, 26), new DateOnly(2026, 6, 26));

        var answer = AggregateSearchHandler.FormatAnswer([("HUF", 123_456m, 4)], range, null);

        Assert.Contains("123,456 HUF", answer);
        Assert.Contains("4 tétel", answer);
        Assert.Contains("2025.12.26", answer);
        Assert.Contains("2026.06.26", answer);
        Assert.DoesNotContain("(a(z)", answer);
    }

    [Fact]
    public void FormatAnswer_WithVendorHint_MentionsHintInParentheses()
    {
        var range = (new DateOnly(2025, 12, 26), new DateOnly(2026, 6, 26));

        var answer = AggregateSearchHandler.FormatAnswer([("HUF", 10_000m, 1)], range, "villany");

        Assert.Contains("(a(z) \"villany\" tételekre)", answer);
    }

    [Fact]
    public void FormatAnswer_MultipleCurrencies_MentionsEveryCurrency()
    {
        // Regression: a mixed HUF+EUR result set previously reported only the numerically
        // dominant currency (HUF), silently dropping the EUR total from the sentence even
        // though its source document was still listed among the hits.
        var range = (new DateOnly(2026, 1, 10), new DateOnly(2026, 7, 10));

        var answer = AggregateSearchHandler.FormatAnswer(
            [("HUF", 147_640m, 2), ("EUR", 1_120m, 1)], range, null);

        Assert.Contains("147,640 HUF", answer);
        Assert.Contains("2 tétel", answer);
        Assert.Contains("1,120 EUR", answer);
        Assert.Contains("1 tétel", answer);
    }
}
