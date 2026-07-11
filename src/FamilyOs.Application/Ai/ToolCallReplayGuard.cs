using System.Collections.Concurrent;
using FamilyOs.Application.Abstractions.Ai;

namespace FamilyOs.Application.Ai;

/// <summary>
/// In-memory (single-instance) implementation of IToolCallReplayGuard. Registered as a
/// singleton — deliberately not IMemoryCache-based to avoid a new package dependency for
/// something this small; a plain ConcurrentDictionary keyed by jti with opportunistic
/// expiry-based pruning is enough for a short (~10 min) TTL window.
/// </summary>
public sealed class ToolCallReplayGuard : IToolCallReplayGuard
{
    private readonly ConcurrentDictionary<Guid, DateTime> _consumed = new();

    public bool TryConsume(Guid jti, DateTime expiresUtc)
    {
        PruneExpired();

        // TryAdd returns false if the key already exists — i.e. this jti was already consumed.
        return _consumed.TryAdd(jti, expiresUtc);
    }

    private void PruneExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var (jti, expiresUtc) in _consumed)
        {
            if (expiresUtc < now)
                _consumed.TryRemove(jti, out _);
        }
    }
}
