using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Ai;
using FamilyOs.Application.Common.Ai;

namespace FamilyOs.Application.Tests.Ai;

public sealed class ToolCallTokenServiceTests
{
    private static ToolCallTokenService BuildService(int ttlSeconds = 600, string key = "test-signing-key-0123456789") =>
        new(new ToolCallTokenOptions { FeatureEnabled = true, SigningKey = key, TtlSeconds = ttlSeconds });

    private static JsonElement SampleArgs() => JsonDocument.Parse("""{"documentId":"11111111-1111-1111-1111-111111111111"}""").RootElement;

    [Fact]
    public void CreateThenValidate_SameUser_RoundTripsSuccessfully()
    {
        var service = BuildService();
        var userId = Guid.NewGuid();

        var (token, expiresUtc) = service.CreateToken("add_tag", SampleArgs(), userId);
        var validation = service.Validate(token, userId);

        validation.Ok.Should().BeTrue();
        validation.Envelope!.Tool.Should().Be("add_tag");
        validation.Envelope.UserAccountId.Should().Be(userId);
        // The wire format encodes exp as whole Unix seconds, so sub-second precision is lost —
        // compare with a 1s tolerance rather than exact equality.
        validation.Envelope.ExpiresUtc.Should().BeCloseTo(expiresUtc, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Validate_DifferentUser_ReturnsUserMismatch()
    {
        var service = BuildService();
        var (token, _) = service.CreateToken("add_tag", SampleArgs(), Guid.NewGuid());

        var validation = service.Validate(token, Guid.NewGuid());

        validation.Ok.Should().BeFalse();
        validation.Error.Should().Be(ToolCallTokenError.UserMismatch);
    }

    [Fact]
    public void Validate_ExpiredToken_ReturnsExpired()
    {
        var service = BuildService(ttlSeconds: -1); // already expired the moment it's issued
        var userId = Guid.NewGuid();
        var (token, _) = service.CreateToken("add_tag", SampleArgs(), userId);

        var validation = service.Validate(token, userId);

        validation.Ok.Should().BeFalse();
        validation.Error.Should().Be(ToolCallTokenError.Expired);
    }

    [Fact]
    public void Validate_TamperedPayload_ReturnsSignatureInvalid()
    {
        var service = BuildService();
        var userId = Guid.NewGuid();
        var (token, _) = service.CreateToken("add_tag", SampleArgs(), userId);

        var parts = token.Split('.');
        var tampered = parts[0] + "x" + "." + parts[1]; // corrupt the payload segment

        var validation = service.Validate(tampered, userId);

        validation.Ok.Should().BeFalse();
        validation.Error.Should().BeOneOf(ToolCallTokenError.SignatureInvalid, ToolCallTokenError.Malformed);
    }

    [Fact]
    public void Validate_SignedByDifferentKey_ReturnsSignatureInvalid()
    {
        var signer = BuildService(key: "key-one-0123456789");
        var verifier = BuildService(key: "key-two-9876543210");
        var userId = Guid.NewGuid();
        var (token, _) = signer.CreateToken("add_tag", SampleArgs(), userId);

        var validation = verifier.Validate(token, userId);

        validation.Ok.Should().BeFalse();
        validation.Error.Should().Be(ToolCallTokenError.SignatureInvalid);
    }

    [Fact]
    public void Validate_MalformedToken_ReturnsMalformed()
    {
        var service = BuildService();

        var validation = service.Validate("not-a-valid-token", Guid.NewGuid());

        validation.Ok.Should().BeFalse();
        validation.Error.Should().Be(ToolCallTokenError.Malformed);
    }

    [Fact]
    public void CreateToken_MissingSigningKey_Throws()
    {
        var service = new ToolCallTokenService(new ToolCallTokenOptions { FeatureEnabled = true, SigningKey = null });

        var act = () => service.CreateToken("add_tag", SampleArgs(), Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }
}
