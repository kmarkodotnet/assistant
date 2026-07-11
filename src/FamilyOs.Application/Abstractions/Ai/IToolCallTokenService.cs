using System.Text.Json;

namespace FamilyOs.Application.Abstractions.Ai;

/// <summary>
/// Decoded, verified content of a proposalToken (ADR-0011 D1). Stateless — never persisted.
/// Jti (a random per-token id, NOT a DB key) exists solely so IToolCallReplayGuard can flag
/// "already executed" within the token's own validity window — no proposal table needed.
/// </summary>
public sealed record ToolCallEnvelope(
    Guid Jti,
    string Tool,
    JsonElement Args,
    Guid UserAccountId,
    DateTime IssuedUtc,
    DateTime ExpiresUtc);

public enum ToolCallTokenError { None, Malformed, Expired, SignatureInvalid, UserMismatch }

public sealed record ToolCallTokenValidation(bool Ok, ToolCallEnvelope? Envelope, ToolCallTokenError Error)
{
    public static ToolCallTokenValidation Success(ToolCallEnvelope envelope) =>
        new(true, envelope, ToolCallTokenError.None);

    public static ToolCallTokenValidation Failure(ToolCallTokenError error) =>
        new(false, null, error);
}

/// <summary>
/// Signs/validates the HMAC-signed, base64url, stateless proposal token (ADR-0011 D1).
/// Signing key comes from TOOLCALL_SIGNING_KEY (env-only, never in the repo).
/// </summary>
public interface IToolCallTokenService
{
    /// <summary>True when the Command mode is enabled (FEATURE_NL_COMMANDS=true).</summary>
    bool FeatureEnabled { get; }

    (string Token, DateTime ExpiresUtc) CreateToken(string toolName, JsonElement resolvedArguments, Guid userAccountId);

    /// <summary>
    /// Validates signature, expiry, and that the token belongs to <paramref name="currentUserAccountId"/>.
    /// Never throws — callers map the result to 401 (Malformed/Expired/SignatureInvalid) or 403 (UserMismatch).
    /// </summary>
    ToolCallTokenValidation Validate(string token, Guid currentUserAccountId);
}

/// <summary>
/// Cheap, process-local replay protection for /tool-calls/confirm (code review finding on
/// c43dd87): the proposalToken is intentionally stateless (ADR-0011 D1 — no proposal table),
/// but create_reminder/create_deadline are NOT idempotent like add_tag/assign_document are, so
/// a double-click or a client retry-after-timeout within the ~10 min TTL window would create
/// duplicate reminders/deadlines. This tracks only a random per-token "jti" for at most its own
/// TTL — no entity data, no DB, single-instance-appropriate (matches this API's current
/// non-clustered deployment; a distributed cache would be the upgrade path if that changes).
/// </summary>
public interface IToolCallReplayGuard
{
    /// <summary>
    /// Returns true the first time <paramref name="jti"/> is seen, false on every call after
    /// that (until <paramref name="expiresUtc"/> passes, after which the token is rejected by
    /// Validate() anyway so the entry is moot).
    /// </summary>
    bool TryConsume(Guid jti, DateTime expiresUtc);
}
