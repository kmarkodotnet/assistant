using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Common.Ai;

namespace FamilyOs.Application.Ai;

/// <summary>
/// HMAC-SHA256-signed, stateless proposal token (ADR-0011 D1). Wire format is a compact,
/// JWT-like "payload.signature" string, both segments base64url — this keeps the signature
/// computation unambiguous (no dependency on JSON property ordering) while still matching
/// the "base64url envelope" shape described in the ADR.
/// </summary>
public sealed class ToolCallTokenService(ToolCallTokenOptions options) : IToolCallTokenService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = null };

    public bool FeatureEnabled => options.FeatureEnabled;

    public (string Token, DateTime ExpiresUtc) CreateToken(string toolName, JsonElement resolvedArguments, Guid userAccountId)
    {
        var iat = DateTime.UtcNow;
        var exp = iat.AddSeconds(options.TtlSeconds);

        var payload = new TokenPayload(1, toolName, resolvedArguments, userAccountId, ToUnixSeconds(iat), ToUnixSeconds(exp));
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOpts);
        var sig = Sign(payloadBytes);

        var token = $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(sig)}";
        return (token, exp);
    }

    public ToolCallTokenValidation Validate(string token, Guid currentUserAccountId)
    {
        var parts = token.Split('.');
        if (parts.Length != 2)
            return ToolCallTokenValidation.Failure(ToolCallTokenError.Malformed);

        byte[] payloadBytes, sigBytes;
        try
        {
            payloadBytes = Base64UrlDecode(parts[0]);
            sigBytes = Base64UrlDecode(parts[1]);
        }
        catch (FormatException)
        {
            return ToolCallTokenValidation.Failure(ToolCallTokenError.Malformed);
        }

        var expectedSig = Sign(payloadBytes);
        if (!CryptographicOperations.FixedTimeEquals(sigBytes, expectedSig))
            return ToolCallTokenValidation.Failure(ToolCallTokenError.SignatureInvalid);

        TokenPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<TokenPayload>(payloadBytes, JsonOpts)
                ?? throw new JsonException("null payload");
        }
        catch (JsonException)
        {
            return ToolCallTokenValidation.Failure(ToolCallTokenError.Malformed);
        }

        var expiresUtc = FromUnixSeconds(payload.Exp);
        if (expiresUtc < DateTime.UtcNow)
            return ToolCallTokenValidation.Failure(ToolCallTokenError.Expired);

        if (payload.Uid != currentUserAccountId)
            return ToolCallTokenValidation.Failure(ToolCallTokenError.UserMismatch);

        var envelope = new ToolCallEnvelope(payload.Tool, payload.Args, payload.Uid, FromUnixSeconds(payload.Iat), expiresUtc);
        return ToolCallTokenValidation.Success(envelope);
    }

    private byte[] Sign(byte[] payloadBytes)
    {
        if (string.IsNullOrEmpty(options.SigningKey))
            throw new InvalidOperationException(
                "TOOLCALL_SIGNING_KEY is not configured — required whenever the Command mode is enabled.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.SigningKey));
        return hmac.ComputeHash(payloadBytes);
    }

    private static long ToUnixSeconds(DateTime utc) => new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeSeconds();
    private static DateTime FromUnixSeconds(long seconds) => DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    private sealed record TokenPayload(int V, string Tool, JsonElement Args, Guid Uid, long Iat, long Exp);
}
