using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Infrastructure.Ai.Options;
using Microsoft.Extensions.Options;

namespace FamilyOs.Infrastructure.Ai.Providers;

public sealed class OllamaEmbedder : IEmbedder
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string ModelName => "nomic-embed-text:v1.5";
    public int Dimensions => 768;

    public OllamaEmbedder(HttpClient http, IOptions<OllamaOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var request = new OllamaEmbeddingRequest
        {
            Model = ModelName,
            Prompt = text,
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/api/embeddings", request, JsonOptions, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new AiProviderUnavailableException($"Ollama embedding request failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new AiProviderUnavailableException("Ollama embedding request timed out.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new AiProviderUnavailableException(
                $"Ollama embedding returned HTTP {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(JsonOptions, ct)
            ?? throw new AiProviderUnavailableException("Ollama embedding returned an empty response.");

        return result.Embedding ?? [];
    }

    public async Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await EmbedAsync(text, ct));
        }
        return [.. results];
    }

    // ----- DTOs -----

    private sealed class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }
}
