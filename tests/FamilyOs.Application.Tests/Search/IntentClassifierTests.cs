using FamilyOs.Application.Search.Intent;

namespace FamilyOs.Application.Tests.Search;

public sealed class IntentClassifierTests
{
    [Theory]
    [InlineData("Mutasd az összes dokumentumot", SearchIntent.Filter)]
    [InlineData("minden feladat", SearchIntent.Filter)]
    [InlineData("Mikor jár le a biztosítás?", SearchIntent.Lookup)]
    [InlineData("mikor jár le a határidő", SearchIntent.Lookup)]
    [InlineData("foglald össze a szerződést", SearchIntent.Summarize)]
    [InlineData("hol találom a számlát", SearchIntent.Find)]
    [InlineData("Mennyi villanyszámlát fizettünk az elmúlt 6 hónapban?", SearchIntent.Aggregate)]
    [InlineData("Összesen mennyit fizettünk a biztosításra?", SearchIntent.Aggregate)]
    [InlineData("hányszor fizettünk elő a Netflixre", SearchIntent.Aggregate)]
    public void Classify_ReturnsExpectedIntent(string query, SearchIntent expectedIntent)
    {
        var (intent, confidence) = IntentClassifier.Classify(query);
        Assert.Equal(expectedIntent, intent);
        Assert.True(confidence > 0);
    }

    [Fact]
    public void Classify_UnknownQuery_ReturnsFind_WithLowConfidence()
    {
        var (intent, confidence) = IntentClassifier.Classify("valami ismeretlen lekérdezés");
        Assert.Equal(SearchIntent.Find, intent);
        Assert.True(confidence < 0.5);
    }
}
