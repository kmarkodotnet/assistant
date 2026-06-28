using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Email;
using FamilyOs.Infrastructure.Ai.Caching;
using FamilyOs.Infrastructure.Ai.Email;
using FamilyOs.Infrastructure.Ai.Extraction;
using FamilyOs.Infrastructure.Ai.Lang;
using FamilyOs.Infrastructure.Ai.Options;
using FamilyOs.Infrastructure.Ai.Providers;
using FamilyOs.Infrastructure.Ai.Search;
using FamilyOs.Infrastructure.Ai.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyOs.Infrastructure.Ai.DependencyInjection;

public static class AiServiceRegistration
{
    public static IServiceCollection AddFamilyOsAiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.Section));
        services.Configure<AiPrivacyOptions>(configuration.GetSection(AiPrivacyOptions.Section));
        services.Configure<TesseractOptions>(configuration.GetSection(TesseractOptions.Section));

        // Ollama HTTP clients — base URL is set in the constructor via IOptions<OllamaOptions>
        services.AddHttpClient<OllamaHttpClient>();
        services.AddHttpClient<OllamaEmbedder>();

        // AI providers
        services.AddSingleton<OllamaAiProvider>();
        services.AddSingleton<IAiProviderFactory, AiProviderFactory>();
        services.AddSingleton<IEmbedder, OllamaEmbedder>();

        // Language detection
        services.AddSingleton<ILanguageDetector, NTextCatLanguageDetector>();

        // Document text extractors
        services.AddScoped<PdfTextLayerExtractor>();
        services.AddScoped<TesseractOcrExtractor>();
        services.AddScoped<IDocumentTextExtractor, CompositeDocumentTextExtractor>();

        // AI content analysis services (scoped)
        services.AddScoped<IDocumentSummarizer, OllamaDocumentSummarizer>();
        services.AddScoped<IDocumentClassifier, OllamaDocumentClassifier>();
        services.AddScoped<IDeadlineExtractor, OllamaDeadlineExtractor>();
        services.AddScoped<ITaskExtractor, OllamaTaskExtractor>();
        services.AddScoped<IWarrantyExtractor, OllamaWarrantyExtractor>();
        services.AddScoped<IMedicalRecordExtractor, OllamaMedicalRecordExtractor>();
        services.AddScoped<IFinancialRecordExtractor, OllamaFinancialRecordExtractor>();

        // Progress notifier — no-op for workers (cross-process SignalR is post-MVP)
        services.AddSingleton<IProcessingProgressNotifier, NoOpProgressNotifier>();

        // Search services (Epic E)
        services.AddMemoryCache();
        services.AddSingleton<IQueryEmbeddingCache, QueryEmbeddingCache>();
        services.AddScoped<ISemanticSearchService, SemanticSearchService>();
        services.AddScoped<IQuestionAnswerService, OllamaQuestionAnswerer>();

        // Email ingestion
        services.AddScoped<IEmailIngestionService, GmailIngestionService>();

        return services;
    }
}
