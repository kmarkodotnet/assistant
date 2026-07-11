using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Ai;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Application.Common.Ai;
using FamilyOs.Application.Tests.Common;
using FamilyOs.Domain.Enums;
using NSubstitute;

namespace FamilyOs.Application.Tests.Ai;

public sealed class ToolCallPlannerTests
{
    private static readonly ToolExecutionContext Ctx =
        new(Guid.NewGuid(), null, "Adult", DateTime.UtcNow, "Europe/Budapest");

    private static ITool FakeTool(ToolResolution resolution)
    {
        var tool = Substitute.For<ITool>();
        tool.Name.Returns("create_reminder");
        tool.Description.Returns("teszt tool");
        tool.JsonSchema.Returns(JsonDocument.Parse("""
            {"type":"object","additionalProperties":false,"required":["anchorRef"],
             "properties":{"anchorRef":{"type":"string","minLength":1,"maxLength":50}}}
            """).RootElement);
        tool.ResolveAsync(Arg.Any<JsonElement>(), Arg.Any<ToolExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(resolution);
        return tool;
    }

    private static ToolCallPlanner BuildPlanner(IAiProvider aiProvider, ITool tool, out IAuditLogger auditLogger)
    {
        var registry = new ToolRegistry([tool]);
        var tokenService = new ToolCallTokenService(
            new ToolCallTokenOptions { FeatureEnabled = true, SigningKey = "test-key-0123456789", TtlSeconds = 600 });
        auditLogger = Substitute.For<IAuditLogger>();
        return new ToolCallPlanner(aiProvider, registry, tokenService, auditLogger);
    }

    [Fact]
    public async Task PlanAsync_ActionNone_FallsBackToQa()
    {
        var provider = new SequencedAiProvider("""{"action":"none","reason":"kérdés, nem utasítás"}""");
        var tool = FakeTool(new ToolResolution(true, JsonDocument.Parse("{}").RootElement, "s", [], [], null));
        var planner = BuildPlanner(provider, tool, out _);

        var result = await planner.PlanAsync("Mikor jár le a biztosításom?", Ctx, default);

        result.Outcome.Should().Be(ToolPlanOutcome.FallbackToQa);
        result.Proposal.Should().BeNull();
    }

    [Fact]
    public async Task PlanAsync_ValidToolCallFirstTry_ReturnsReadyProposal()
    {
        var provider = new SequencedAiProvider("""
            {"action":"tool_call","tool":"create_reminder","arguments":{"anchorRef":"mosógép"},"userConfirmationText":"x"}
            """);
        var resolution = new ToolResolution(
            true, JsonDocument.Parse("""{"anchorRef":"mosógép"}""").RootElement,
            "Emlékeztetőt hozok létre.", [new ToolParamDisplay("Termék", "mosógép")], [], null);
        var tool = FakeTool(resolution);
        var planner = BuildPlanner(provider, tool, out _);

        var result = await planner.PlanAsync("Emlékeztess a mosógép garanciájára", Ctx, default);

        result.Outcome.Should().Be(ToolPlanOutcome.Ready);
        result.Proposal.Should().NotBeNull();
        result.Proposal!.ToolName.Should().Be("create_reminder");
        result.Proposal.Summary.Should().Be("Emlékeztetőt hozok létre.");
        provider.CallCount.Should().Be(1); // no retry needed
    }

    [Fact]
    public async Task PlanAsync_BadJsonThenValid_RetriesOnceAndSucceeds()
    {
        var provider = new SequencedAiProvider(
            "ez nem JSON, csak sima szöveg",
            """{"action":"tool_call","tool":"create_reminder","arguments":{"anchorRef":"mosógép"},"userConfirmationText":"x"}""");
        var resolution = new ToolResolution(
            true, JsonDocument.Parse("""{"anchorRef":"mosógép"}""").RootElement, "s", [], [], null);
        var tool = FakeTool(resolution);
        var planner = BuildPlanner(provider, tool, out _);

        var result = await planner.PlanAsync("Emlékeztess a mosógép garanciájára", Ctx, default);

        result.Outcome.Should().Be(ToolPlanOutcome.Ready);
        provider.CallCount.Should().Be(2); // exactly one retry
    }

    [Fact]
    public async Task PlanAsync_BadJsonBothTimes_ParseFailed_AndLogsAiCallFailure()
    {
        var provider = new SequencedAiProvider("teljesen érthetetlen kimenet", "még mindig érthetetlen");
        var tool = FakeTool(new ToolResolution(true, JsonDocument.Parse("{}").RootElement, "s", [], [], null));
        var planner = BuildPlanner(provider, tool, out var auditLogger);

        var result = await planner.PlanAsync("valami furcsa üzenet", Ctx, default);

        result.Outcome.Should().Be(ToolPlanOutcome.ParseFailed);
        result.Proposal.Should().BeNull();
        result.Message.Should().NotBeNullOrWhiteSpace();
        provider.CallCount.Should().Be(2);

        await auditLogger.Received(1).LogAsync(
            AuditAction.AiCall, Ctx.UserAccountId, "ToolCallPlanner",
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanAsync_ResolveFails_ReturnsResolveFailedWithMessageNoProposal()
    {
        var provider = new SequencedAiProvider(
            """{"action":"tool_call","tool":"create_reminder","arguments":{"anchorRef":"ismeretlen"},"userConfirmationText":"x"}""");
        var tool = FakeTool(ToolResolution.Failure("Nem található feladatot ezzel a névvel: \"ismeretlen\"."));
        var planner = BuildPlanner(provider, tool, out _);

        var result = await planner.PlanAsync("Emlékeztess valamire", Ctx, default);

        result.Outcome.Should().Be(ToolPlanOutcome.ResolveFailed);
        result.Proposal.Should().BeNull();
        result.Message.Should().Contain("Nem található");
    }
}
