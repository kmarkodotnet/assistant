using MediatR;
using Microsoft.Extensions.Configuration;

namespace FamilyOs.Application.Admin.AiProviders;

public sealed class ListAiProvidersQueryHandler(IConfiguration configuration)
    : IRequestHandler<ListAiProvidersQuery, IReadOnlyList<AiProviderDto>>
{
    public Task<IReadOnlyList<AiProviderDto>> Handle(ListAiProvidersQuery request, CancellationToken cancellationToken)
    {
        var section = configuration.GetSection("Ai:Providers");
        var providers = new List<AiProviderDto>();

        if (section.Exists())
        {
            foreach (var child in section.GetChildren())
            {
                var name = child.Key;
                var enabled = child.GetValue<bool>("Enabled");
                var model = child["Model"];
                var lastHealth = child["LastHealth"];
                providers.Add(new AiProviderDto(name, enabled, model, lastHealth));
            }
        }

        if (providers.Count == 0)
        {
            var ollamaEnabled = configuration.GetValue<bool?>("Ai:OllamaEnabled") ?? true;
            var ollamaModel = configuration["Ai:DefaultModel"] ?? configuration["Ollama:Model"] ?? "llama3";
            providers.Add(new AiProviderDto("Ollama", ollamaEnabled, ollamaModel, null));
        }

        IReadOnlyList<AiProviderDto> result = providers;
        return Task.FromResult(result);
    }
}
