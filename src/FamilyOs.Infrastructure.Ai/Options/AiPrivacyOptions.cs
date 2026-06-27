namespace FamilyOs.Infrastructure.Ai.Options;

public enum PrivacyMode { LocalOnly, HybridAllowed, AnyProvider }

public sealed class AiPrivacyOptions
{
    public const string Section = "Ai";
    public PrivacyMode PrivacyMode { get; set; } = PrivacyMode.LocalOnly;
}
