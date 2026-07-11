namespace FamilyOs.Application.Common.Ai;

/// <summary>
/// Built once at startup (DependencyInjection.AddApplication) directly from flat env vars —
/// FEATURE_NL_COMMANDS / TOOLCALL_SIGNING_KEY / TOOLCALL_PROPOSAL_TTL_SECONDS (ADR-0011 D1).
/// These are NOT nested under a config section, so they are read as top-level keys rather
/// than bound via services.Configure&lt;T&gt;(section) like the other *Options classes.
/// </summary>
public sealed class ToolCallTokenOptions
{
    public bool FeatureEnabled { get; init; }
    public string? SigningKey { get; init; }
    public int TtlSeconds { get; init; } = 600;
}
