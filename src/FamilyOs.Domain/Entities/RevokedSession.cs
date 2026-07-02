namespace FamilyOs.Domain.Entities;

public sealed class RevokedSession
{
    private RevokedSession() { }

    public Guid Id { get; private set; }
    public string SessionId { get; private set; } = string.Empty;
    public DateTime RevokedUtc { get; private set; }

    public static RevokedSession Create(string sessionId, DateTime revokedUtc)
        => new()
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            RevokedUtc = revokedUtc,
        };
}
