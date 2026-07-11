using System.Text.Json;

namespace FamilyOs.Application.Abstractions.Ai;

/// <summary>
/// Decoded, verified content of a proposalToken (ADR-0011 D1). Stateless — never persisted.
/// </summary>
public sealed record ToolCallEnvelope(
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
