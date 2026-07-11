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

public sealed class AssignDocumentToolTests
{
    private static readonly ToolExecutionContext Ctx =
        new(Guid.NewGuid(), null, "Adult", DateTime.UtcNow, "Europe/Budapest");

    private static Document CreateDocument(string title) =>
        Document.Create(title, "file.pdf", "application/pdf", 100, "/tmp/x", "sha", SourceType.Upload, Origin.Manual, Guid.NewGuid());

    private static FamilyMember CreateMember(string displayName) =>
        FamilyMember.Create(displayName, Relation.Child);

    private static IFamilyOsDbContext BuildDb(List<Document> documents, List<FamilyMember> members)
    {
        var db = Substitute.For<IFamilyOsDbContext>();
        var documentSet = MockDbSet.Create(documents);
        var memberSet = MockDbSet.Create(members);
        db.Documents.Returns(documentSet);
        db.FamilyMembers.Returns(memberSet);
        return db;
    }

    private static JsonElement RawArgs(string documentRef, string familyMemberRef) =>
        JsonDocument.Parse($$"""{"documentRef":"{{documentRef}}","familyMemberRef":"{{familyMemberRef}}"}""").RootElement;

    [Fact]
    public async Task ResolveAsync_HappyPath_ResolvesDocumentAndFamilyMember()
    {
        var doc = CreateDocument("Suli bizonyítvány");
        var member = CreateMember("Kata");
        var db = BuildDb([doc], [member]);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();
        auth.CanReadDocument(Arg.Any<Document>()).Returns(true);

        var tool = new AssignDocumentTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("Suli bizonyítvány", "Kata"), Ctx, default);

        result.Ok.Should().BeTrue();
        result.ResolvedArguments.GetProperty("documentId").GetGuid().Should().Be(doc.Id);
        result.ResolvedArguments.GetProperty("relatedFamilyMemberId").GetGuid().Should().Be(member.Id);
    }

    [Fact]
    public async Task ResolveAsync_AmbiguousFamilyMemberName_ReturnsFailure()
    {
        var doc = CreateDocument("Suli bizonyítvány");
        // Neither exactly equals "Kata" — both only contain it — so RefMatcher's
        // exact-match-first pass finds nothing and falls through to the substring pass,
        // where both match, which is the actual ambiguous case worth testing.
        var kata1 = CreateMember("Kis Kata");
        var kata2 = CreateMember("Nagy Kata");
        var db = BuildDb([doc], [kata1, kata2]);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();
        auth.CanReadDocument(Arg.Any<Document>()).Returns(true);

        var tool = new AssignDocumentTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("Suli bizonyítvány", "Kata"), Ctx, default);

        result.Ok.Should().BeFalse();
        result.Error.Should().Contain("Több családtag");
    }

    [Fact]
    public async Task ResolveAsync_FamilyMemberNotFound_ReturnsFailure()
    {
        var doc = CreateDocument("Suli bizonyítvány");
        var db = BuildDb([doc], []);
        var auth = Substitute.For<IFamilyOsAuthorizationService>();
        auth.CanReadDocument(Arg.Any<Document>()).Returns(true);

        var tool = new AssignDocumentTool(Substitute.For<ISender>(), db, auth);

        var result = await tool.ResolveAsync(RawArgs("Suli bizonyítvány", "Ismeretlen"), Ctx, default);

        result.Ok.Should().BeFalse();
        result.Error.Should().Contain("Nem található családtag");
    }

    [Fact]
    public async Task ExecuteAsync_SendsPatchDocumentCommandWithNullRowVersion()
    {
        var documentId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        var resolved = JsonDocument.Parse(
            $$"""{"documentId":"{{documentId}}","relatedFamilyMemberId":"{{memberId}}"}""").RootElement;

        var tool = new AssignDocumentTool(sender, Substitute.For<IFamilyOsDbContext>(), Substitute.For<IFamilyOsAuthorizationService>());

        var result = await tool.ExecuteAsync(resolved, Ctx, default);

        result.ResultType.Should().Be("Document");
        result.ResultId.Should().Be(documentId);
        await sender.Received(1).Send(
            Arg.Is<FamilyOs.Application.Documents.PatchDocument.PatchDocumentCommand>(
                c => c.DocumentId == documentId && c.RelatedFamilyMemberId == memberId && c.RowVersion == null),
            Arg.Any<CancellationToken>());
    }
}
