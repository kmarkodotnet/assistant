using System.Net;
using System.Text;
using System.Text.Json;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Ai.Email;
using FamilyOs.Infrastructure.Ai.Options;
using FamilyOs.Infrastructure.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FamilyOs.Infrastructure.Tests.Email;

/// <summary>
/// Unit tests for GmailIngestionService.SyncAsync().
/// Uses NSubstitute-backed IFamilyOsDbContext with a MockDbSet helper to avoid
/// standing up Testcontainers (kept fast and offline-capable).
/// </summary>
public sealed class GmailIngestionServiceTests
{
    // ----- Builder helpers ------------------------------------------------

    private static GmailIngestionService BuildService(
        IFamilyOsDbContext db,
        IAuditLogger? auditLogger = null,
        IHttpClientFactory? httpFactory = null,
        GmailOptions? gmailOpts = null)
    {
        auditLogger ??= Substitute.For<IAuditLogger>();
        httpFactory ??= Substitute.For<IHttpClientFactory>();
        gmailOpts ??= new GmailOptions { ClientId = "test-client-id", ClientSecret = "test-secret" };

        return new GmailIngestionService(
            db,
            auditLogger,
            httpFactory,
            Options.Create(gmailOpts),
            NullLogger<GmailIngestionService>.Instance);
    }

    private static IFamilyOsDbContext BuildDbContextWithSource(Source? source)
    {
        var db = Substitute.For<IFamilyOsDbContext>();

        var sourcesList = source is not null ? new List<Source> { source } : new List<Source>();
        var sourcesSet = MockDbSet.Create(sourcesList);
        db.Sources.Returns(sourcesSet);

        // EmailMessages — empty by default
        var emailSet = MockDbSet.Create(new List<EmailMessage>());
        db.EmailMessages.Returns(emailSet);

        // AiProcessingJobs — track Add calls
        var jobSet = MockDbSet.Create(new List<AiProcessingJob>());
        db.AiProcessingJobs.Returns(jobSet);

        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        return db;
    }

    private static Source CreateGmailSource(string configJson = "{\"refresh_token\":\"rt-test\"}")
        => Source.Create("Gmail", SourceKind.GmailAccount, configJson);

    // ----- HttpMessageHandler stubs ---------------------------------------

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public void Enqueue(HttpStatusCode status, string json)
            => _responses.Enqueue(new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.TryDequeue(out var resp))
                return Task.FromResult(resp);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"no stub response queued\"}")
            });
        }
    }

    private static IHttpClientFactory BuildFactory(StubHttpHandler handler)
    {
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(client);
        return factory;
    }

    // ----- Serialization helpers ------------------------------------------

    private static string TokenJson(string accessToken = "at-test")
        => JsonSerializer.Serialize(new { access_token = accessToken });

    private static string MessageListJson(params string[] ids)
    {
        var messages = ids.Select(id => new { id, threadId = "thread-" + id });
        return JsonSerializer.Serialize(new { messages });
    }

    private static string MessageDetailJson(
        string id,
        string subject = "Hello",
        string from = "sender@example.com",
        string to = "me@example.com",
        string bodyText = "plain body",
        long internalDate = 1_700_000_000_000L)
    {
        return JsonSerializer.Serialize(new
        {
            id,
            threadId = "t-" + id,
            snippet = "short snippet",
            internalDate,
            payload = new
            {
                mimeType = "multipart/alternative",
                headers = new[]
                {
                    new { name = "Subject", value = subject },
                    new { name = "From", value = from },
                    new { name = "To", value = to },
                },
                parts = new[]
                {
                    new
                    {
                        mimeType = "text/plain",
                        filename = (string?)null,
                        headers = Array.Empty<object>(),
                        body = new { data = Base64Url(bodyText), size = bodyText.Length },
                        parts = Array.Empty<object>()
                    }
                }
            }
        });
    }

    private static string Base64Url(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    // =====================================================================
    // Test cases
    // =====================================================================

    [Fact]
    public async Task SyncAsync_SourceNotFound_ReturnsErrorReport()
    {
        // Arrange
        var db = BuildDbContextWithSource(null);
        var svc = BuildService(db);

        // Act
        var report = await svc.SyncAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        report.Fetched.Should().Be(0);
        report.Inserted.Should().Be(0);
        report.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SyncAsync_WrongSourceKind_ReturnsErrorReport()
    {
        // Arrange
        var source = Source.Create("Upload", SourceKind.Upload, "{}");
        var db = BuildDbContextWithSource(source);
        var svc = BuildService(db);

        // Act
        var report = await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        report.Error.Should().Contain("not a Gmail account");
    }

    [Fact]
    public async Task SyncAsync_MissingRefreshToken_ReturnsErrorReport()
    {
        // Arrange — ConfigJson has no refresh_token
        var source = CreateGmailSource("{}");
        var db = BuildDbContextWithSource(source);
        var svc = BuildService(db);

        // Act
        var report = await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        report.Error.Should().Contain("refresh_token");
    }

    [Fact]
    public async Task SyncAsync_MissingClientCredentials_ReturnsErrorReport()
    {
        // Arrange — GmailOptions has empty credentials
        var source = CreateGmailSource();
        var db = BuildDbContextWithSource(source);
        var svc = BuildService(db, gmailOpts: new GmailOptions { ClientId = "", ClientSecret = "" });

        // Act
        var report = await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        report.Error.Should().Contain("ClientId");
    }

    [Fact]
    public async Task SyncAsync_TokenRefreshFails_ReturnsErrorReport()
    {
        // Arrange
        var source = CreateGmailSource();
        var db = BuildDbContextWithSource(source);

        var handler = new StubHttpHandler();
        handler.Enqueue(HttpStatusCode.Unauthorized, "{\"error\":\"invalid_grant\"}");

        var svc = BuildService(db, httpFactory: BuildFactory(handler));

        // Act
        var report = await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        report.Error.Should().Contain("access token");
    }

    [Fact]
    public async Task SyncAsync_MessageListFetchFails_ReturnsErrorReport()
    {
        // Arrange
        var source = CreateGmailSource();
        var db = BuildDbContextWithSource(source);

        var handler = new StubHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, TokenJson());
        handler.Enqueue(HttpStatusCode.Forbidden, "{\"error\":\"forbidden\"}");

        var svc = BuildService(db, httpFactory: BuildFactory(handler));

        // Act
        var report = await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        report.Error.Should().Contain("message list");
    }

    [Fact]
    public async Task SyncAsync_EmptyMessageList_ReturnsZeroReport()
    {
        // Arrange
        var source = CreateGmailSource();
        var db = BuildDbContextWithSource(source);

        var handler = new StubHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, TokenJson());
        handler.Enqueue(HttpStatusCode.OK, JsonSerializer.Serialize(new { })); // no messages field

        var svc = BuildService(db, httpFactory: BuildFactory(handler));

        // Act
        var report = await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        report.Fetched.Should().Be(0);
        report.Inserted.Should().Be(0);
        report.Skipped.Should().Be(0);
        report.Error.Should().BeNull();
    }

    [Fact]
    public async Task SyncAsync_NewMessages_InsertsEmailMessageAndAiJob()
    {
        // Arrange
        var source = CreateGmailSource();
        var db = BuildDbContextWithSource(source);

        var insertedMessages = new List<EmailMessage>();
        var insertedJobs = new List<AiProcessingJob>();
        db.EmailMessages.When(s => s.Add(Arg.Any<EmailMessage>()))
            .Do(c => insertedMessages.Add(c.Arg<EmailMessage>()));
        db.AiProcessingJobs.When(s => s.Add(Arg.Any<AiProcessingJob>()))
            .Do(c => insertedJobs.Add(c.Arg<AiProcessingJob>()));

        var handler = new StubHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, TokenJson());
        handler.Enqueue(HttpStatusCode.OK, MessageListJson("msg-001"));
        handler.Enqueue(HttpStatusCode.OK, MessageDetailJson("msg-001", subject: "Test subject"));

        var svc = BuildService(db, httpFactory: BuildFactory(handler));

        // Act
        var report = await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        report.Fetched.Should().Be(1);
        report.Inserted.Should().Be(1);
        report.Skipped.Should().Be(0);
        report.Error.Should().BeNull();

        insertedMessages.Should().HaveCount(1);
        insertedMessages[0].GmailMessageId.Should().Be("msg-001");
        insertedMessages[0].Subject.Should().Be("Test subject");
        insertedMessages[0].FromAddress.Should().Be("sender@example.com");
        insertedMessages[0].BodyText.Should().Be("plain body");

        insertedJobs.Should().HaveCount(1);
        insertedJobs[0].JobType.Should().Be(AiJobType.ExtractText);
        insertedJobs[0].TargetType.Should().Be(JobTargetType.EmailMessage);

        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_DuplicateMessage_SkipsAndDoesNotInsert()
    {
        // Arrange
        var source = CreateGmailSource();

        // Existing message with the same GmailMessageId
        var existingMessage = EmailMessage.Create(
            source.Id, "msg-001", "a@b.com", "c@d.com", "Old", DateTime.UtcNow, false);

        var db = Substitute.For<IFamilyOsDbContext>();
        var sourcesSet = MockDbSet.Create(new List<Source> { source });
        db.Sources.Returns(sourcesSet);

        var emailSet = MockDbSet.Create(new List<EmailMessage> { existingMessage });
        db.EmailMessages.Returns(emailSet);

        var jobSet = MockDbSet.Create(new List<AiProcessingJob>());
        db.AiProcessingJobs.Returns(jobSet);
        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var handler = new StubHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, TokenJson());
        handler.Enqueue(HttpStatusCode.OK, MessageListJson("msg-001")); // same id

        var svc = BuildService(db, httpFactory: BuildFactory(handler));

        // Act
        var report = await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        report.Fetched.Should().Be(1);
        report.Skipped.Should().Be(1);
        report.Inserted.Should().Be(0);

        // SaveChanges IS still called once to persist LastSyncUtc even when no messages inserted
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_MessageFetchFails_SkipsIndividualMessage()
    {
        // Arrange
        var source = CreateGmailSource();
        var db = BuildDbContextWithSource(source);

        var handler = new StubHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, TokenJson());
        handler.Enqueue(HttpStatusCode.OK, MessageListJson("msg-fail"));
        handler.Enqueue(HttpStatusCode.NotFound, "{}"); // message detail fails

        var svc = BuildService(db, httpFactory: BuildFactory(handler));

        // Act
        var report = await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        report.Fetched.Should().Be(1);
        report.Skipped.Should().Be(1);
        report.Inserted.Should().Be(0);
    }

    [Fact]
    public async Task SyncAsync_MultipleMessages_InsertsOnlyNew()
    {
        // Arrange
        var source = CreateGmailSource();

        var existingMsg = EmailMessage.Create(
            source.Id, "msg-old", "a@b.com", "c@d.com", "Old", DateTime.UtcNow, false);

        var db = Substitute.For<IFamilyOsDbContext>();
        db.Sources.Returns(MockDbSet.Create(new List<Source> { source }));
        db.EmailMessages.Returns(MockDbSet.Create(new List<EmailMessage> { existingMsg }));

        var insertedMessages = new List<EmailMessage>();
        db.EmailMessages.When(s => s.Add(Arg.Any<EmailMessage>()))
            .Do(c => insertedMessages.Add(c.Arg<EmailMessage>()));
        db.AiProcessingJobs.Returns(MockDbSet.Create(new List<AiProcessingJob>()));
        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new StubHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, TokenJson());
        handler.Enqueue(HttpStatusCode.OK, MessageListJson("msg-old", "msg-new")); // 2 messages
        // Only msg-new requires a detail fetch (msg-old is skipped as duplicate)
        handler.Enqueue(HttpStatusCode.OK, MessageDetailJson("msg-new", subject: "New message"));

        var svc = BuildService(db, httpFactory: BuildFactory(handler));

        // Act
        var report = await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        report.Fetched.Should().Be(2);
        report.Inserted.Should().Be(1);
        report.Skipped.Should().Be(1);

        insertedMessages.Should().HaveCount(1);
        insertedMessages[0].GmailMessageId.Should().Be("msg-new");
    }

    [Fact]
    public async Task SyncAsync_AuditLogWritten_Always()
    {
        // Arrange
        var source = CreateGmailSource();
        var db = BuildDbContextWithSource(source);
        var auditLogger = Substitute.For<IAuditLogger>();

        var handler = new StubHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, TokenJson());
        handler.Enqueue(HttpStatusCode.OK, MessageListJson()); // empty list

        var svc = BuildService(db, auditLogger: auditLogger, httpFactory: BuildFactory(handler));

        // Act
        await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert
        await auditLogger.Received(1).LogAsync(
            AuditAction.ExternalApiCall,
            null,
            "Source",
            source.Id,
            detailsJson: Arg.Any<string>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_CallsUpdateLastSync_ViaDbSave()
    {
        // The service calls Source.UpdateLastSync() + SaveChangesAsync() at the end of every
        // successful sync (even when no new messages) so that LastSyncUtc is persisted.

        var source = CreateGmailSource();
        var db = BuildDbContextWithSource(source);

        var insertedMessages = new List<EmailMessage>();
        db.EmailMessages.When(s => s.Add(Arg.Any<EmailMessage>()))
            .Do(c => insertedMessages.Add(c.Arg<EmailMessage>()));

        var handler = new StubHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, TokenJson());
        handler.Enqueue(HttpStatusCode.OK, MessageListJson("msg-001"));
        handler.Enqueue(HttpStatusCode.OK, MessageDetailJson("msg-001"));

        var svc = BuildService(db, httpFactory: BuildFactory(handler));

        // Act
        await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert — SaveChanges called exactly once (inserts + LastSync together)
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_NoNewMessages_SaveChangesCalledForLastSync()
    {
        // Arrange — empty message list: no inserts, but LastSyncUtc must still be persisted
        var source = CreateGmailSource();
        var db = BuildDbContextWithSource(source);

        var handler = new StubHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, TokenJson());
        handler.Enqueue(HttpStatusCode.OK, JsonSerializer.Serialize(new { }));

        var svc = BuildService(db, httpFactory: BuildFactory(handler));

        // Act
        await svc.SyncAsync(source.Id, CancellationToken.None);

        // Assert — SaveChanges still called to persist the LastSyncUtc update
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
