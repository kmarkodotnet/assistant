using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Ai.Tools;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Application.Tests.Common;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using NSubstitute;

namespace FamilyOs.Application.Tests.Ai.Tools;

public sealed class AddDocumentTagToolTests
{
    private static readonly ToolExecutionContext Ctx =
        new(Guid.NewGuid(), null, "Adult", DateTime.UtcNow, "Europe/Budapest");

    private static Document CreateDocument(string title) =>
        Document.Create(title, "file.pdf", "application/pdf", 100, "/tmp/x", "sha", SourceType.Upload, Origin.Manual, Guid.NewGuid());

    private static JsonElement RawArgs(string documentRef, string tagName) =>
        JsonDocument.Parse($$"""{"documentRef":"{{documentRef}}","tagName":"{{tagName}}"}""").RootElement;

    private static IFamilyOsDbContext BuildDb(List<Document> documents, List<Tag> tags)
    {
        var db = Substitute.For<IFamilyOsDbContext>();

        // Evaluate MockDbSet.Create() into locals first — see CreateReminderToolTests.BuildDb
        // for why inlining it directly as a .Returns() argument breaks NSubstitute's tracking.
        var documentSet = MockDbSet.Create(documents);
        var tagSet = MockDbSet.Create(tags);

        db.Documents.Returns(documentSet);
        db.Tags.Returns(tagSet);
        return db;
    }

    [Fact]
    public async Task ResolveAsync_ExistingDocumentAndTag_ReturnsOkWithResolvedIds()
    {
        var doc = CreateDocument("Biztosítási kötvény");
        var tag = Tag.Create("axa");
        var db = BuildDb([doc], [tag]);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();
        auth.CanReadDocument(Arg.Any<Document>()).Returns(true);

        var tool = new AddDocumentTagTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("Biztosítási kötvény", "axa"), Ctx, default);

        result.Ok.Should().BeTrue();
        result.ResolvedArguments.GetProperty("documentId").GetGuid().Should().Be(doc.Id);
        result.ResolvedArguments.GetProperty("tagId").GetGuid().Should().Be(tag.Id);
        result.Display.Should().Contain(d => d.Label == "Címke" && d.Value == "axa");
    }

    [Fact]
    public async Task ResolveAsync_TagDoesNotExist_ReturnsFailureWithoutGuessing()
    {
        var doc = CreateDocument("Biztosítási kötvény");
        var db = BuildDb([doc], []);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();
        auth.CanReadDocument(Arg.Any<Document>()).Returns(true);

        var tool = new AddDocumentTagTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("Biztosítási kötvény", "nemletezo"), Ctx, default);

        result.Ok.Should().BeFalse();
        result.Error.Should().Contain("nemletezo");
    }

    [Fact]
    public async Task ResolveAsync_AmbiguousDocumentTitle_ReturnsFailure()
    {
        var doc1 = CreateDocument("Biztosítás 2025");
        var doc2 = CreateDocument("Biztosítás 2026");
        var tag = Tag.Create("axa");
        var db = BuildDb([doc1, doc2], [tag]);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();
        auth.CanReadDocument(Arg.Any<Document>()).Returns(true);

        var tool = new AddDocumentTagTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("Biztosítás", "axa"), Ctx, default);

        result.Ok.Should().BeFalse();
        result.Error.Should().Contain("Több dokumentum");
    }

    [Fact]
    public async Task ResolveAsync_DocumentNotVisibleToUser_IsTreatedAsNotFound()
    {
        var doc = CreateDocument("Titkos dokumentum");
        var tag = Tag.Create("axa");
        var db = BuildDb([doc], [tag]);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();
        auth.CanReadDocument(Arg.Any<Document>()).Returns(false); // not visible to this user

        var tool = new AddDocumentTagTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("Titkos dokumentum", "axa"), Ctx, default);

        result.Ok.Should().BeFalse();
        result.Error.Should().Contain("Nem található dokumentum");
    }

    [Fact]
    public async Task ExecuteAsync_SendsAddDocumentTagCommandAndReturnsResult()
    {
        var documentId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        var resolved = JsonDocument.Parse($$"""{"documentId":"{{documentId}}","tagId":"{{tagId}}"}""").RootElement;

        var tool = new AddDocumentTagTool(sender, Substitute.For<IFamilyOsDbContext>(), Substitute.For<IFamilyOsAuthorizationService>());

        var result = await tool.ExecuteAsync(resolved, Ctx, default);

        result.ResultType.Should().Be("Document");
        result.ResultId.Should().Be(documentId);
        await sender.Received(1).Send(
            Arg.Is<FamilyOs.Application.Documents.AddDocumentTag.AddDocumentTagCommand>(
                c => c.DocumentId == documentId && c.TagId == tagId),
            Arg.Any<CancellationToken>());
    }
}
