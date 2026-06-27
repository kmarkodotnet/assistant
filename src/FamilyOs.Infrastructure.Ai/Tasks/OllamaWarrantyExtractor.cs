using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Tasks;

public sealed class OllamaWarrantyExtractor : IWarrantyExtractor
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<OllamaWarrantyExtractor> _logger;

    private static readonly Action<ILogger, Exception?> LogExtractDone =
        LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(LogExtractDone)),
            "OllamaWarrantyExtractor: extraction complete.");

    public OllamaWarrantyExtractor(IAiProviderFactory providerFactory, ILogger<OllamaWarrantyExtractor> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<WarrantyExtraction?> ExtractAsync(string text, CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider();
        var template = PromptTemplate.Load(PromptCatalog.ExtractWarranty);
        var truncated = text.Length > 12000 ? text[..12000] : text;

        var userPrompt = PromptTemplate.Replace(template, new Dictionary<string, string>
        {
            ["document"] = truncated,
        });

        var prompt = new AiPrompt(
            SystemPrompt: "You are a helpful assistant that extracts warranty information from documents.",
            UserPrompt: userPrompt,
            PromptId: PromptCatalog.ExtractWarranty,
            PromptVersion: PromptCatalog.GetVersion(PromptCatalog.ExtractWarranty));

        var completion = await provider.CompleteAsync(prompt, ct);

        LogExtractDone(_logger, null);
        return ParseResult(completion.Content);
    }

    private static WarrantyExtraction? ParseResult(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var productName = root.TryGetProperty("productName", out var pn) ? pn.GetString() : null;
            var purchaseDateStr = root.TryGetProperty("purchaseDate", out var pd) ? pd.GetString() : null;
            var expiryDateStr = root.TryGetProperty("expiryDate", out var ed) ? ed.GetString() : null;
            var warrantyMonths = root.TryGetProperty("warrantyMonths", out var wm) && wm.ValueKind == JsonValueKind.Number
                ? wm.GetInt32() : (int?)null;
            var notes = root.TryGetProperty("notes", out var n) ? n.GetString() : null;

            DateOnly? purchaseDate = TryParseDate(purchaseDateStr);
            DateOnly? expiryDate = TryParseDate(expiryDateStr);

            return new WarrantyExtraction(productName, purchaseDate, expiryDate, warrantyMonths, notes);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DateOnly? TryParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr) || dateStr == "null")
            return null;
        return DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", out var d) ? d : null;
    }
}
