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
