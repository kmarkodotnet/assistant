namespace FamilyOs.Domain.Entities;

public sealed class PendingInvite
{
    private PendingInvite() { }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public Guid FamilyMemberId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public DateTime CreatedUtc { get; private set; }

    public static PendingInvite Create(string email, Guid familyMemberId, string role, DateTime now)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Email = email.ToLowerInvariant().Trim(),
            FamilyMemberId = familyMemberId,
            Role = role,
            CreatedUtc = now,
        };
}
