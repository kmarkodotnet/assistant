namespace FamilyOs.Application.Auth.Options;

public sealed class AuthOptions
{
    public string GoogleClientId { get; set; } = string.Empty;
    public string[] AllowedEmails { get; set; } = Array.Empty<string>();
    public string BootstrapAdmin { get; set; } = string.Empty;
}
