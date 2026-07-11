using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Ai.Tools;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Reminders.Dtos;
using FamilyOs.Application.Tests.Common;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using NSubstitute;

namespace FamilyOs.Application.Tests.Ai.Tools;

public sealed class CreateReminderToolTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly ToolExecutionContext Ctx =
        new(UserId, null, "Adult", new DateTime(2026, 7, 11, 8, 0, 0, DateTimeKind.Utc), "Europe/Budapest");

    private static Document CreateDocument(Guid createdBy) =>
        Document.Create("Mosógép garancia", "file.pdf", "application/pdf", 100, "/tmp/x", "sha",
            SourceType.Upload, Origin.Manual, createdBy);

    private static IFamilyOsDbContext BuildDb(
        List<FamilyTask>? tasks = null, List<Deadline>? deadlines = null,
        List<Warranty>? warranties = null, List<Document>? documents = null)
    {
        var db = Substitute.For<IFamilyOsDbContext>();

        // NSubstitute tracks "the last call awaiting Returns()" — MockDbSet.Create() itself
        // makes substitute calls internally, so it must be evaluated into a local BEFORE
        // calling .Returns() on the outer substitute, or the wrong call gets configured.
        var taskSet = MockDbSet.Create(tasks ?? []);
        var deadlineSet = MockDbSet.Create(deadlines ?? []);
        var warrantySet = MockDbSet.Create(warranties ?? []);
        var documentSet = MockDbSet.Create(documents ?? []);

        db.Tasks.Returns(taskSet);
        db.Deadlines.Returns(deadlineSet);
        db.Warranties.Returns(warrantySet);
        db.Documents.Returns(documentSet);
        return db;
    }

    private static JsonElement RawArgs(string anchorType, string anchorRef, int offsetDays) =>
        JsonDocument.Parse(
            $$"""{"anchorType":"{{anchorType}}","anchorRef":"{{anchorRef}}","offsetDays":{{offsetDays}}}""")
            .RootElement;

    [Fact]
    public async Task ResolveAsync_WarrantyAnchor_HappyPath_ComputesTriggerBeforeExpiry()
    {
        var document = CreateDocument(UserId);
        var warranty = Warranty.Create(document.Id, "Bosch WAT28 mosógép");
        warranty.Patch(null, null, null, null, null, null, null, null,
            warrantyEndDate: new DateOnly(2027, 3, 1), seller: null, relatedFamilyMemberId: null);

        var db = BuildDb(warranties: [warranty], documents: [document]);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();
        auth.CanReadDocument(Arg.Any<Document>()).Returns(true);

        var tool = new CreateReminderTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("warranty", "mosógép", 3), Ctx, default);

        result.Ok.Should().BeTrue();
        result.ResolvedArguments.GetProperty("sourceDocumentId").GetGuid().Should().Be(document.Id);
        result.ResolvedArguments.GetProperty("dueDateUtc").GetDateTime().Should().Be(new DateTime(2027, 3, 1, 8, 0, 0, DateTimeKind.Utc));
        result.ResolvedArguments.GetProperty("triggerUtc").GetDateTime().Should().Be(new DateTime(2027, 2, 26, 8, 0, 0, DateTimeKind.Utc));
        result.Display.Should().Contain(d => d.Label == "Termék");
    }

    [Fact]
    public async Task ResolveAsync_WarrantyAnchor_AmbiguousProductName_ReturnsFailure()
    {
        var document = CreateDocument(UserId);
        var w1 = Warranty.Create(document.Id, "Bosch mosógép");
        w1.Patch(null, null, null, null, null, null, null, null, new DateOnly(2027, 1, 1), null, null);
        var w2 = Warranty.Create(document.Id, "Bosch mosogatógép");
        w2.Patch(null, null, null, null, null, null, null, null, new DateOnly(2027, 1, 1), null, null);

        var db = BuildDb(warranties: [w1, w2], documents: [document]);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();
        auth.CanReadDocument(Arg.Any<Document>()).Returns(true);

        var tool = new CreateReminderTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("warranty", "Bosch", 3), Ctx, default);

        result.Ok.Should().BeFalse();
        result.Error.Should().Contain("Több");
    }

    [Fact]
    public async Task ResolveAsync_WarrantyAnchor_NotVisibleToUser_ReturnsNotFound()
    {
        var document = CreateDocument(Guid.NewGuid()); // some other user
        var warranty = Warranty.Create(document.Id, "Bosch mosógép");
        warranty.Patch(null, null, null, null, null, null, null, null, new DateOnly(2027, 1, 1), null, null);

        var db = BuildDb(warranties: [warranty], documents: [document]);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();
        auth.CanReadDocument(Arg.Any<Document>()).Returns(false);

        var tool = new CreateReminderTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("warranty", "Bosch", 3), Ctx, default);

        result.Ok.Should().BeFalse();
        result.Error.Should().Contain("Nem található");
    }

    [Fact]
    public async Task ResolveAsync_TaskAnchor_NotFound_ReturnsFailure()
    {
        var db = BuildDb(tasks: []);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();

        var tool = new CreateReminderTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("task", "Nem létező feladat", 1), Ctx, default);

        result.Ok.Should().BeFalse();
        result.Error.Should().Contain("Nem található");
    }

    [Fact]
    public async Task ExecuteAsync_TaskAnchor_SendsCreateReminderCommandOnly()
    {
        var taskId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<FamilyOs.Application.Reminders.CreateReminderCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ReminderDto { Id = Guid.NewGuid(), TaskId = taskId });

        var tool = new CreateReminderTool(sender, Substitute.For<IFamilyOsDbContext>(), Substitute.For<IFamilyOsAuthorizationService>());

        var resolved = JsonDocument.Parse($$"""
            {
              "anchorType": "task",
              "taskId": "{{taskId}}",
              "triggerUtc": "2026-08-01T09:00:00Z",
              "channel": "inApp",
              "recurrence": null,
              "targetUserAccountId": "{{UserId}}",
              "createdByUserId": "{{UserId}}"
            }
            """).RootElement;

        var result = await tool.ExecuteAsync(resolved, Ctx, default);

        result.ResultType.Should().Be("Reminder");
        await sender.Received(1).Send(
            Arg.Is<FamilyOs.Application.Reminders.CreateReminderCommand>(c => c.TaskId == taskId && c.DeadlineId == null),
            Arg.Any<CancellationToken>());
        await sender.DidNotReceive().Send(Arg.Any<FamilyOs.Application.Deadlines.CreateDeadlineCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WarrantyAnchor_ChainsCreateDeadlineThenCreateReminder()
    {
        var deadlineId = Guid.NewGuid();
        var reminderId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<FamilyOs.Application.Deadlines.CreateDeadlineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FamilyOs.Application.Deadlines.Dtos.DeadlineDto { Id = deadlineId });
        sender.Send(Arg.Any<FamilyOs.Application.Reminders.CreateReminderCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ReminderDto { Id = reminderId, DeadlineId = deadlineId });

        var tool = new CreateReminderTool(sender, Substitute.For<IFamilyOsDbContext>(), Substitute.For<IFamilyOsAuthorizationService>());

        var resolved = JsonDocument.Parse($$"""
            {
              "anchorType": "warranty",
              "sourceDocumentId": "{{Guid.NewGuid()}}",
              "productName": "Bosch mosógép",
              "warrantyEndDate": "2027-03-01",
              "dueDateUtc": "2027-03-01T08:00:00Z",
              "triggerUtc": "2027-02-26T08:00:00Z",
              "channel": "inApp",
              "recurrence": null,
              "targetUserAccountId": "{{UserId}}",
              "createdByUserId": "{{UserId}}"
            }
            """).RootElement;

        var result = await tool.ExecuteAsync(resolved, Ctx, default);

        result.ResultType.Should().Be("Reminder");
        result.ResultId.Should().Be(reminderId);
        await sender.Received(1).Send(Arg.Any<FamilyOs.Application.Deadlines.CreateDeadlineCommand>(), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(
            Arg.Is<FamilyOs.Application.Reminders.CreateReminderCommand>(c => c.DeadlineId == deadlineId && c.TaskId == null),
            Arg.Any<CancellationToken>());
    }
}
