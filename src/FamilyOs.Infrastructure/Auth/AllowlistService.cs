using FamilyOs.Application.Abstractions.Auth;
using Microsoft.Extensions.Configuration;

namespace FamilyOs.Infrastructure.Auth;

public sealed class AllowlistService : IAllowlistService
{
    private readonly HashSet<string> _allowedEmails;

    public AllowlistService(IConfiguration configuration)
    {
        var emails = configuration.GetSection("Auth:AllowedEmails").Get<string[]>()
                     ?? Array.Empty<string>();
        var bootstrap = configuration["Auth:BootstrapAdmin"];

        _allowedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var email in emails)
        {
            _allowedEmails.Add(email.Trim());
        }
        if (!string.IsNullOrWhiteSpace(bootstrap))
            _allowedEmails.Add(bootstrap.Trim());
    }

    public bool IsEmailAllowed(string email)
        => !string.IsNullOrWhiteSpace(email) && _allowedEmails.Contains(email.Trim());
}
