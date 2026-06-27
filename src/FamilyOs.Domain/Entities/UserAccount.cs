using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class UserAccount
{
    private UserAccount() { } // EF Core

    public Guid Id { get; private set; }
    public Guid FamilyMemberId { get; private set; }
    public string GoogleSubject { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime? LastLoginUtc { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    // User preferences
    public bool EmailEnabled { get; private set; } = true;
    public string? QuietHoursStart { get; private set; }
    public string? QuietHoursEnd { get; private set; }

    // Navigation
    public FamilyMember FamilyMember { get; private set; } = null!;

    public static UserAccount Create(
        Guid familyMemberId,
        string googleSubject,
        string email,
        string displayName,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(googleSubject))
            throw new ArgumentException("GoogleSubject is required.", nameof(googleSubject));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return new UserAccount
        {
            Id = Guid.NewGuid(),
            FamilyMemberId = familyMemberId,
            GoogleSubject = googleSubject,
            Email = email.ToLowerInvariant().Trim(),
            DisplayName = displayName.Trim(),
            Role = role,
            IsActive = true,
            EmailEnabled = true,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };
    }

    public void RecordLogin() => LastLoginUtc = DateTime.UtcNow;

    public void ChangeRole(UserRole newRole) => Role = newRole;

    public void SetActive(bool active) => IsActive = active;

    public void SoftDelete() => DeletedUtc = DateTime.UtcNow;

    public void UpdatePreferences(bool emailEnabled, string? quietHoursStart, string? quietHoursEnd)
    {
        EmailEnabled = emailEnabled;
        QuietHoursStart = quietHoursStart;
        QuietHoursEnd = quietHoursEnd;
    }
}
