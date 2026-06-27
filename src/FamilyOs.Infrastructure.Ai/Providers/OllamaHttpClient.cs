using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Infrastructure.Ai.Options;
using Microsoft.Extensions.Options;

namespace FamilyOs.Infrastructure.Ai.Providers;

public sealed class OllamaHttpClient
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public OllamaHttpClient(HttpClient http, IOptions<OllamaOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<(string Content, int InputTokens, int OutputTokens)> PostChatAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default)
    {
        var request = new OllamaChatRequest
        {
            Model = model,
            Messages =
            [
                new OllamaMessage { Role = "system", Content = systemPrompt },
                new OllamaMessage { Role = "user",   Content = userPrompt   },
            ],
            Stream = false,
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("/api/chat", request, JsonOptions, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new AiProviderUnavailableException($"Ollama chat request failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new AiProviderUnavailableException("Ollama chat request timed out.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new AiProviderUnavailableException(
                $"Ollama returned HTTP {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, ct)
            ?? throw new AiProviderUnavailableException("Ollama returned an empty response.");

        return (
            Content: result.Message?.Content ?? string.Empty,
            InputTokens: result.PromptEvalCount,
            OutputTokens: result.EvalCount
        );
    }

    // ----- DTOs -----

    private sealed class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaMessage> Messages { get; set; } = [];

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }
    }
}
