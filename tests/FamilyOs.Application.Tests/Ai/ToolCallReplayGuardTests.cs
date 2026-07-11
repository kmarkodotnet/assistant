using FamilyOs.Application.Ai;

namespace FamilyOs.Application.Tests.Ai;

public sealed class ToolCallReplayGuardTests
{
    [Fact]
    public void TryConsume_FirstTime_ReturnsTrue()
    {
        var guard = new ToolCallReplayGuard();

        var result = guard.TryConsume(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(5));

        result.Should().BeTrue();
    }

    [Fact]
    public void TryConsume_SameJtiTwice_SecondCallReturnsFalse()
    {
        var guard = new ToolCallReplayGuard();
        var jti = Guid.NewGuid();
        var expiresUtc = DateTime.UtcNow.AddMinutes(5);

        var first = guard.TryConsume(jti, expiresUtc);
        var second = guard.TryConsume(jti, expiresUtc);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public void TryConsume_DifferentJtis_BothReturnTrue()
    {
        var guard = new ToolCallReplayGuard();
        var expiresUtc = DateTime.UtcNow.AddMinutes(5);

        var first = guard.TryConsume(Guid.NewGuid(), expiresUtc);
        var second = guard.TryConsume(Guid.NewGuid(), expiresUtc);

        first.Should().BeTrue();
        second.Should().BeTrue();
    }

    [Fact]
    public void TryConsume_PrunesEntriesPastTheirOwnExpiry()
    {
        // Not a correctness requirement in itself (an expired token is already rejected by
        // Validate() before TryConsume is ever called) — just confirms the dictionary doesn't
        // grow unbounded by keeping already-expired jtis around forever.
        var guard = new ToolCallReplayGuard();
        var alreadyExpiredJti = Guid.NewGuid();
        guard.TryConsume(alreadyExpiredJti, DateTime.UtcNow.AddSeconds(-1));

        // A later call (with a different, still-valid jti) triggers the opportunistic prune.
        guard.TryConsume(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(5));

        // The expired entry should have been pruned, so re-consuming it looks "fresh" again —
        // harmless in practice since Validate() would have already rejected that token as
        // Expired well before this could matter.
        var reconsumed = guard.TryConsume(alreadyExpiredJti, DateTime.UtcNow.AddMinutes(5));
        reconsumed.Should().BeTrue();
    }
}
