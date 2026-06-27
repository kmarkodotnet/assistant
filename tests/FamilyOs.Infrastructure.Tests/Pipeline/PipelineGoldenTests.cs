using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Options;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using FamilyOs.Infrastructure.Ai.Tasks;
using FamilyOs.Infrastructure.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FamilyOs.Infrastructure.Tests.Pipeline;

/// <summary>
/// Pipeline golden tests using InMemoryAiProvider stub.
/// Tests the AI content analysis services in isolation (no real Ollama needed).
/// </summary>
public sealed class PipelineGoldenTests
{
    private static StubAiProviderFactory CreateFactoryWithStub(Dictionary<string, string> responses)
    {
        var inMemoryProvider = new InMemoryAiProvider(responses);
        return new StubAiProviderFactory(inMemoryProvider);
    }

    [Fact]
    public async Task Pipeline_WithStubProvider_ProducesExpectedSummary()
    {
        // Arrange
        var responses = new Dictionary<string, string>
        {
            [PromptCatalog.Summarize] = "{\"summary\": \"Ez egy teszt összefoglaló.\"}",
        };
        var factory = CreateFactoryWithStub(responses);
        var summarizer = new OllamaDocumentSummarizer(factory, NullLogger<OllamaDocumentSummarizer>.Instance);

        // Act
        var result = await summarizer.SummarizeAsync("Teszt dokumentum szövege.", "hu");

        // Assert
        result.Summary.Should().Be("Ez egy teszt összefoglaló.");
        result.ModelName.Should().Be("in-memory");
        result.PromptVersion.Should().Be("v1");
    }

    [Fact]
    public async Task Pipeline_WithStubProvider_ProducesExpectedClassification()
    {
        // Arrange
        var responses = new Dictionary<string, string>
        {
            [PromptCatalog.Classify] = "{\"topics\": [\"Biztosítás\"], \"tags\": [\"axa\", \"kötelező\"], \"facetType\": \"Financial\"}",
        };
        var factory = CreateFactoryWithStub(responses);
        var classifier = new OllamaDocumentClassifier(factory, NullLogger<OllamaDocumentClassifier>.Instance);

        // Act
        var result = await classifier.ClassifyAsync("Biztosítási dokumentum szövege.");

        // Assert
        result.Topics.Should().ContainSingle("Biztosítás");
        result.Tags.Should().Contain("axa");
        result.Tags.Should().Contain("kötelező");
        result.FacetType.Should().Be("Financial");
    }

    [Fact]
    public async Task Pipeline_WithStubProvider_ClassifyNullFacetType_ReturnsNullFacetType()
    {
        // Arrange
        var responses = new Dictionary<string, string>
        {
            [PromptCatalog.Classify] = "{\"topics\": [], \"tags\": [\"általános\"], \"facetType\": \"null\"}",
        };
        var factory = CreateFactoryWithStub(responses);
        var classifier = new OllamaDocumentClassifier(factory, NullLogger<OllamaDocumentClassifier>.Instance);

        // Act
        var result = await classifier.ClassifyAsync("Általános dokumentum.");

        // Assert
        result.FacetType.Should().BeNull();
    }

    [Fact]
    public async Task Pipeline_WithStubProvider_ExtractsDeadlines_OnlyFutureDates()
    {
        // Arrange: response contains one past date (should be filtered) and one future date
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var futureDate = today.AddMonths(6).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var pastDate = today.AddMonths(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var responses = new Dictionary<string, string>
        {
            [PromptCatalog.ExtractDeadlines] = $"{{\"deadlines\": [" +
                $"{{\"title\": \"Jövőbeli határidő\", \"dueDate\": \"{futureDate}\", \"description\": \"Fontos\"}}, " +
                $"{{\"title\": \"Múltbeli határidő\", \"dueDate\": \"{pastDate}\", \"description\": \"Nem kell\"}}" +
                $"]}}",
        };
        var factory = CreateFactoryWithStub(responses);
        var extractor = new OllamaDeadlineExtractor(factory, NullLogger<OllamaDeadlineExtractor>.Instance);

        // Act
        var results = await extractor.ExtractAsync("Dokumentum", today);

        // Assert: only future deadline returned
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Jövőbeli határidő");
    }

    [Fact]
    public async Task Pipeline_WithStubProvider_ExtractsTasks()
    {
        // Arrange
        var responses = new Dictionary<string, string>
        {
            [PromptCatalog.ExtractTasks] = "{\"tasks\": [" +
                "{\"title\": \"Fizesse ki a számlát\", \"assignedToHint\": \"Apa\", \"dueDate\": null, \"description\": \"Fontos\"}" +
                "]}",
        };
        var factory = CreateFactoryWithStub(responses);
        var extractor = new OllamaTaskExtractor(factory, NullLogger<OllamaTaskExtractor>.Instance);

        // Act
        var results = await extractor.ExtractAsync("Számla dokumentum.", ["Apa", "Anya"]);

        // Assert
        results.Should().ContainSingle();
        results[0].Title.Should().Be("Fizesse ki a számlát");
        results[0].AssignedToHint.Should().Be("Apa");
        results[0].DueDate.Should().BeNull();
    }

    [Fact]
    public async Task Pipeline_WithStubProvider_ExtractsWarranty()
    {
        // Arrange
        var responses = new Dictionary<string, string>
        {
            [PromptCatalog.ExtractWarranty] = "{\"productName\": \"Samsung TV\", \"purchaseDate\": \"2026-01-15\", " +
                "\"expiryDate\": \"2028-01-15\", \"warrantyMonths\": 24, \"notes\": \"Eredeti jótállás\"}",
        };
        var factory = CreateFactoryWithStub(responses);
        var extractor = new OllamaWarrantyExtractor(factory, NullLogger<OllamaWarrantyExtractor>.Instance);

        // Act
        var result = await extractor.ExtractAsync("Jótállási jegy szövege.");

        // Assert
        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Samsung TV");
        result.WarrantyMonths.Should().Be(24);
        result.PurchaseDate.Should().Be(new DateOnly(2026, 1, 15));
    }

    [Fact]
    public async Task Pipeline_WithStubProvider_ExtractsMedicalRecord()
    {
        // Arrange
        var responses = new Dictionary<string, string>
        {
            [PromptCatalog.ExtractMedical] = "{\"recordType\": \"Prescription\", \"doctorName\": \"Dr. Kovács\", " +
                "\"recordDate\": \"2026-06-01\", \"diagnosis\": \"Magas vérnyomás\", \"notes\": \"3 hónapra szól\"}",
        };
        var factory = CreateFactoryWithStub(responses);
        var extractor = new OllamaMedicalRecordExtractor(factory, NullLogger<OllamaMedicalRecordExtractor>.Instance);

        // Act
        var result = await extractor.ExtractAsync("Receptszöveg.");

        // Assert
        result.Should().NotBeNull();
        result!.RecordType.Should().Be("Prescription");
        result.DoctorName.Should().Be("Dr. Kovács");
        result.Diagnosis.Should().Be("Magas vérnyomás");
    }

    [Fact]
    public async Task Pipeline_WithStubProvider_ExtractsFinancialRecord()
    {
        // Arrange
        var responses = new Dictionary<string, string>
        {
            [PromptCatalog.ExtractFinancial] = "{\"amount\": 15000.0, \"currency\": \"HUF\", " +
                "\"recordDate\": \"2026-06-15\", \"isPaid\": true, \"recurrencePeriod\": \"Monthly\", \"notes\": \"\"}",
        };
        var factory = CreateFactoryWithStub(responses);
        var extractor = new OllamaFinancialRecordExtractor(factory, NullLogger<OllamaFinancialRecordExtractor>.Instance);

        // Act
        var result = await extractor.ExtractAsync("Számla szövege.");

        // Assert
        result.Should().NotBeNull();
        result!.Amount.Should().Be(15000m);
        result.Currency.Should().Be("HUF");
        result.IsPaid.Should().BeTrue();
        result.RecurrencePeriod.Should().Be("Monthly");
    }

    [Fact]
    public async Task Pipeline_WithStubProvider_InvalidJson_ReturnsSafeDefault()
    {
        // Arrange
        var responses = new Dictionary<string, string>
        {
            [PromptCatalog.Summarize] = "This is not valid JSON",
        };
        var factory = CreateFactoryWithStub(responses);
        var summarizer = new OllamaDocumentSummarizer(factory, NullLogger<OllamaDocumentSummarizer>.Instance);

        // Act
        var result = await summarizer.SummarizeAsync("Dokumentum.", "hu");

        // Assert: should not throw, returns raw content as fallback
        result.Should().NotBeNull();
        result.Summary.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Stub provider factory that always returns the in-memory provider.
    /// </summary>
    private sealed class StubAiProviderFactory : IAiProviderFactory
    {
        private readonly IAiProvider _provider;
        public StubAiProviderFactory(IAiProvider provider) => _provider = provider;
        public IAiProvider GetProvider() => _provider;
    }
}
