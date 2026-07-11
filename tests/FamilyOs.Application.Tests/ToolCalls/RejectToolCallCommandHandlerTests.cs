using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Application.ToolCalls;
using FamilyOs.Domain.Enums;
using NSubstitute;

namespace FamilyOs.Application.Tests.ToolCalls;

public sealed class RejectToolCallCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly JsonElement Args = JsonDocument.Parse("{}").RootElement;

    private static ICurrentUserAccessor CurrentUser()
    {
        var accessor = Substitute.For<ICurrentUserAccessor>();
        accessor.UserAccountId.Returns(UserId);
        return accessor;
    }

    [Fact]
    public async Task Handle_ValidToken_WritesRejectAuditWithToolNameAndNoOtherSideEffects()
    {
        var tokenService = Substitute.For<IToolCallTokenService>();
        var envelope = new ToolCallEnvelope(Guid.NewGuid(), "create_reminder", Args, UserId, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));
        tokenService.Validate("tok", UserId).Returns(ToolCallTokenValidation.Success(envelope));

        var auditLogger = Substitute.For<IAuditLogger>();
        var handler = new RejectToolCallCommandHandler(tokenService, CurrentUser(), auditLogger);

        await handler.Handle(new RejectToolCallCommand("tok", "Nem ezt akartam"), default);

        await auditLogger.Received(1).LogAsync(
            AuditAction.Reject, UserId, "ToolCall:create_reminder",
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MalformedOrExpiredToken_StillWritesRejectAudit_NoThrow()
    {
        var tokenService = Substitute.For<IToolCallTokenService>();
        tokenService.Validate("bad-token", UserId).Returns(ToolCallTokenValidation.Failure(ToolCallTokenError.Expired));

        var auditLogger = Substitute.For<IAuditLogger>();
        var handler = new RejectToolCallCommandHandler(tokenService, CurrentUser(), auditLogger);

        var act = async () => await handler.Handle(new RejectToolCallCommand("bad-token", null), default);

        await act.Should().NotThrowAsync();
        await auditLogger.Received(1).LogAsync(
            AuditAction.Reject, UserId, "ToolCall:unknown",
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
