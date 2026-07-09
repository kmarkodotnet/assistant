namespace FamilyOs.Infrastructure.Ai.Options;

public sealed class GmailOptions
{
    public const string Section = "Gmail";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}
