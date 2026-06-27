namespace FamilyOs.Application.Abstractions.Ai;

public interface ILanguageDetector
{
    /// <summary>
    /// Detects the language of the given text.
    /// </summary>
    /// <returns>ISO 639-1 language code (e.g. "hu", "en"), or "unknown" if detection fails.</returns>
    string Detect(string text);
}
