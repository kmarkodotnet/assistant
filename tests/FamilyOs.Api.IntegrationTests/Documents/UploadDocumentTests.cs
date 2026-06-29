using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace FamilyOs.Api.IntegrationTests.Documents;

[Trait("Category", "Integration")]
[Collection("IntegrationTests")]
public sealed class UploadDocumentTests(FamilyOsTestFixture fixture)
{
    [Fact]
    public async Task Upload_ValidPdf_Returns201()
    {
        // Arrange: login first
        var loginResp = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login/google", new { idToken = "admin-token" });
        loginResp.EnsureSuccessStatusCode();

        // Upload a minimal PDF (magic bytes: %PDF-1.4)
        using var ms = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 });
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(ms), "file", "test.pdf");

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/documents") { Content = form };
        req.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        // Act
        var resp = await fixture.Client.SendAsync(req);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Upload_DuplicateFile_Returns409()
    {
        // Arrange: login first
        var loginResp = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login/google", new { idToken = "admin-token" });
        loginResp.EnsureSuccessStatusCode();

        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x35 };

        async Task<HttpResponseMessage> Upload()
        {
            using var ms = new MemoryStream(pdfBytes);
            using var form = new MultipartFormDataContent();
            form.Add(new StreamContent(ms), "file", "dup.pdf");
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/documents") { Content = form };
            req.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            return await fixture.Client.SendAsync(req);
        }

        // Act
        var first = await Upload();
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var second = await Upload();

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListDocuments_AfterUpload_ReturnsItems()
    {
        // Arrange
        var loginResp = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login/google", new { idToken = "admin-token" });
        loginResp.EnsureSuccessStatusCode();

        using var ms = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x36 });
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(ms), "file", "list-test.pdf");
        var uploadReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/documents") { Content = form };
        uploadReq.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var uploadResp = await fixture.Client.SendAsync(uploadReq);
        uploadResp.EnsureSuccessStatusCode();

        // Act
        var listResp = await fixture.Client.GetAsync("/api/v1/documents");

        // Assert
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
