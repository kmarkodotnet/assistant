using FamilyOs.Application.Abstractions.Ai;
using NTextCat;

namespace FamilyOs.Infrastructure.Ai.Lang;

/// <summary>
/// Language detector backed by NTextCat (trigram-based statistical classifier).
/// Returns ISO 639-1 codes (e.g. "hu", "en"). Falls back to "unknown" if the
/// language profile is unavailable (e.g., in unit-test environments).
/// </summary>
public sealed class NTextCatLanguageDetector : ILanguageDetector
{
    // Profile file name — loaded from the output directory at runtime.
    private const string ProfileFileName = "Core14.profile.xml";

    private static readonly Lazy<RankedLanguageIdentifier?> LazyIdentifier =
        new(TryCreateIdentifier, LazyThreadSafetyMode.PublicationOnly);

    public string Detect(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "unknown";

        try
        {
            var identifier = LazyIdentifier.Value;
            if (identifier is null)
                return "unknown";

            // Identify from first 1000 chars for performance
            var snippet = text.Length > 1000 ? text[..1000] : text;
            var languages = identifier.Identify(snippet);
            var best = languages.FirstOrDefault();

            if (best is null)
                return "unknown";

            // NTextCat returns ISO 639-3; map to ISO 639-1
            return MapToIso6391(best.Item1.Iso639_3);
        }
        catch
        {
            return "unknown";
        }
    }

    private static RankedLanguageIdentifier? TryCreateIdentifier()
    {
        try
        {
            // Look for the profile next to the executing assembly (CopyToOutputDirectory)
            var baseDir = AppContext.BaseDirectory;
            var profilePath = Path.Combine(baseDir, ProfileFileName);

            if (!File.Exists(profilePath))
            {
                // Fallback: look relative to current directory
                profilePath = Path.Combine(Directory.GetCurrentDirectory(), ProfileFileName);
            }

            if (!File.Exists(profilePath))
                return null;

            var factory = new RankedLanguageIdentifierFactory();
            return factory.Load(profilePath);
        }
        catch
        {
            return null;
        }
    }

    private static string MapToIso6391(string? iso6393)
    {
        if (string.IsNullOrEmpty(iso6393))
            return "unknown";

        return iso6393 switch
        {
            "hun" => "hu",
            "eng" => "en",
            "deu" => "de",
            "fra" => "fr",
            "spa" => "es",
            "ita" => "it",
            "pol" => "pl",
            "ces" => "cs",
            "slk" => "sk",
            "ron" => "ro",
            "rus" => "ru",
            "ukr" => "uk",
            "srp" => "sr",
            "hrv" => "hr",
            "slv" => "sl",
            "bul" => "bg",
            "nld" => "nl",
            "por" => "pt",
            "swe" => "sv",
            "dan" => "da",
            "nor" => "no",
            "fin" => "fi",
            "tur" => "tr",
            "zho" => "zh",
            "jpn" => "ja",
            "kor" => "ko",
            _ => iso6393.Length >= 2 ? iso6393[..2] : iso6393,
        };
    }
}
