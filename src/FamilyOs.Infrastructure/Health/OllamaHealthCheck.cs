using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FamilyOs.Infrastructure.Health;

public sealed class OllamaHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("ollama");
            using var response = await client.GetAsync("/", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Ollama is reachable.")
                : HealthCheckResult.Degraded($"Ollama returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Healthy($"Ollama unreachable (non-blocking): {ex.Message}");
        }
    }
}
