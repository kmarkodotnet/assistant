using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Tasks;

public sealed class OllamaMedicalRecordExtractor : IMedicalRecordExtractor
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<OllamaMedicalRecordExtractor> _logger;

    private static readonly Action<ILogger, Exception?> LogExtractDone =
        LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(LogExtractDone)),
            "OllamaMedicalRecordExtractor: extraction complete.");

    public OllamaMedicalRecordExtractor(IAiProviderFactory providerFactory, ILogger<OllamaMedicalRecordExtractor> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<MedicalRecordExtraction?> ExtractAsync(string text, CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider();
        var template = PromptTemplate.Load(PromptCatalog.ExtractMedical);
        var truncated = text.Length > 12000 ? text[..12000] : text;

        var userPrompt = PromptTemplate.Replace(template, new Dictionary<string, string>
        {
            ["document"] = truncated,
        });

        var prompt = new AiPrompt(
            SystemPrompt: "You are a helpful assistant that extracts medical record information from documents.",
            UserPrompt: userPrompt,
            PromptId: PromptCatalog.ExtractMedical,
            PromptVersion: PromptCatalog.GetVersion(PromptCatalog.ExtractMedical));

        var completion = await provider.CompleteAsync(prompt, ct);

        LogExtractDone(_logger, null);
        return ParseResult(completion.Content);
    }

    private static MedicalRecordExtraction? ParseResult(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var recordType = root.TryGetProperty("recordType", out var rt) ? rt.GetString() : null;
            var doctorName = root.TryGetProperty("doctorName", out var dn) ? dn.GetString() : null;
            var recordDateStr = root.TryGetProperty("recordDate", out var rd) ? rd.GetString() : null;
            var diagnosis = root.TryGetProperty("diagnosis", out var diag) ? diag.GetString() : null;
            var notes = root.TryGetProperty("notes", out var n) ? n.GetString() : null;

            DateOnly? recordDate = null;
            if (!string.IsNullOrWhiteSpace(recordDateStr) && recordDateStr != "null")
            {
                if (DateOnly.TryParseExact(recordDateStr, "yyyy-MM-dd", out var parsed))
                    recordDate = parsed;
            }

            return new MedicalRecordExtraction(recordType, doctorName, recordDate, diagnosis, notes);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
