using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Application.ToolCalls;
using FamilyOs.Domain.Enums;
using NSubstitute;

namespace FamilyOs.Application.Tests.ToolCalls;

public sealed class ConfirmToolCallCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly JsonElement Args = JsonDocument.Parse("{}").RootElement;

    private static ICurrentUserAccessor CurrentUser()
    {
        var accessor = Substitute.For<ICurrentUserAccessor>();
        accessor.UserAccountId.Returns(UserId);
        accessor.Role.Returns("Adult");
        return accessor;
    }

    // Default: every jti is fresh (allows the pre-existing happy-path tests to focus on their
    // own concern). The dedicated replay test below configures this differently.
    private static IToolCallReplayGuard AllowingReplayGuard()
    {
        var guard = Substitute.For<IToolCallReplayGuard>();
        guard.TryConsume(Arg.Any<Guid>(), Arg.Any<DateTime>()).Returns(true);
        return guard;
    }

    [Fact]
    public async Task Handle_ValidToken_ExecutesToolAndWritesApproveAudit()
    {
        var tokenService = Substitute.For<IToolCallTokenService>();
        var envelope = new ToolCallEnvelope(Guid.NewGuid(), "add_tag", Args, UserId, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));
        tokenService.Validate("tok", UserId).Returns(ToolCallTokenValidation.Success(envelope));

        var resultId = Guid.NewGuid();
        var tool = Substitute.For<ITool>();
        tool.Name.Returns("add_tag");
        tool.ExecuteAsync(Args, Arg.Any<ToolExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(new ToolResult("Document", resultId, "A címke hozzáadása megtörtént."));

        var registry = Substitute.For<IToolRegistry>();
        registry.TryGet("add_tag", out Arg.Any<ITool>()!).Returns(x =>
        {
            x[1] = tool;
            return true;
        });

        var auditLogger = Substitute.For<IAuditLogger>();
        var handler = new ConfirmToolCallCommandHandler(tokenService, AllowingReplayGuard(), registry, CurrentUser(), auditLogger);

        var result = await handler.Handle(new ConfirmToolCallCommand("tok"), default);

        result.Executed.Should().BeTrue();
        result.ResultType.Should().Be("Document");
        result.ResultId.Should().Be(resultId);

        await auditLogger.Received(1).LogAsync(
            AuditAction.Approve, UserId, "ToolCall:add_tag", resultId,
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredOrInvalidToken_ThrowsUnauthorized()
    {
        var tokenService = Substitute.For<IToolCallTokenService>();
        tokenService.Validate("tok", UserId).Returns(ToolCallTokenValidation.Failure(ToolCallTokenError.Expired));

        var handler = new ConfirmToolCallCommandHandler(
            tokenService, AllowingReplayGuard(), Substitute.For<IToolRegistry>(), CurrentUser(), Substitute.For<IAuditLogger>());

        var act = async () => await handler.Handle(new ConfirmToolCallCommand("tok"), default);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_TokenBelongsToDifferentUser_ThrowsForbidden()
    {
        var tokenService = Substitute.For<IToolCallTokenService>();
        tokenService.Validate("tok", UserId).Returns(ToolCallTokenValidation.Failure(ToolCallTokenError.UserMismatch));

        var handler = new ConfirmToolCallCommandHandler(
            tokenService, AllowingReplayGuard(), Substitute.For<IToolRegistry>(), CurrentUser(), Substitute.For<IAuditLogger>());

        var act = async () => await handler.Handle(new ConfirmToolCallCommand("tok"), default);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_EntityGoneAtExecuteTime_ThrowsBusinessRuleNot404()
    {
        var tokenService = Substitute.For<IToolCallTokenService>();
        var envelope = new ToolCallEnvelope(Guid.NewGuid(), "add_tag", Args, UserId, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));
        tokenService.Validate("tok", UserId).Returns(ToolCallTokenValidation.Success(envelope));

        var tool = Substitute.For<ITool>();
        tool.Name.Returns("add_tag");
        tool.ExecuteAsync(Args, Arg.Any<ToolExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns<ToolResult>(_ => throw new NotFoundException("Document", Guid.NewGuid()));

        var registry = Substitute.For<IToolRegistry>();
        registry.TryGet("add_tag", out Arg.Any<ITool>()!).Returns(x =>
        {
            x[1] = tool;
            return true;
        });

        var handler = new ConfirmToolCallCommandHandler(
            tokenService, AllowingReplayGuard(), registry, CurrentUser(), Substitute.For<IAuditLogger>());

        var act = async () => await handler.Handle(new ConfirmToolCallCommand("tok"), default);

        await act.Should().ThrowAsync<DomainBusinessRuleException>();
    }

    [Fact]
    public async Task Handle_TokenAlreadyConsumed_ThrowsConflict_AndDoesNotExecuteTwice()
    {
        // The idempotency gap the code review flagged: a valid, non-expired token submitted a
        // second time (double-click, client retry) must NOT execute create_reminder again.
        var tokenService = Substitute.For<IToolCallTokenService>();
        var jti = Guid.NewGuid();
        var envelope = new ToolCallEnvelope(jti, "create_reminder", Args, UserId, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));
        tokenService.Validate("tok", UserId).Returns(ToolCallTokenValidation.Success(envelope));

        var replayGuard = Substitute.For<IToolCallReplayGuard>();
        replayGuard.TryConsume(jti, envelope.ExpiresUtc).Returns(false); // already consumed

        var tool = Substitute.For<ITool>();
        var handler = new ConfirmToolCallCommandHandler(
            tokenService, replayGuard, Substitute.For<IToolRegistry>(), CurrentUser(), Substitute.For<IAuditLogger>());

        var act = async () => await handler.Handle(new ConfirmToolCallCommand("tok"), default);

        await act.Should().ThrowAsync<ConflictException>();
        await tool.DidNotReceive().ExecuteAsync(Arg.Any<JsonElement>(), Arg.Any<ToolExecutionContext>(), Arg.Any<CancellationToken>());
    }
}
