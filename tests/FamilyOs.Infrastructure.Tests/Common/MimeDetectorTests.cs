using FamilyOs.Infrastructure.Common;
using FluentAssertions;

namespace FamilyOs.Infrastructure.Tests.Common;

public sealed class MimeDetectorTests
{
    private readonly MimeDetector _sut = new();

    [Fact]
    public void DetectMimeType_PdfMagicBytes_ReturnsPdf()
    {
        // Arrange: %PDF-
        var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        using var ms = new MemoryStream(bytes);

        // Act
        var result = _sut.DetectMimeType(ms);

        // Assert
        result.Should().Be("application/pdf");
    }

    [Fact]
    public void DetectMimeType_JpegMagicBytes_ReturnsJpeg()
    {
        // Arrange
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        using var ms = new MemoryStream(bytes);

        // Act
        var result = _sut.DetectMimeType(ms);

        // Assert
        result.Should().Be("image/jpeg");
    }

    [Fact]
    public void DetectMimeType_PngMagicBytes_ReturnsPng()
    {
        // Arrange
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var ms = new MemoryStream(bytes);

        // Act
        var result = _sut.DetectMimeType(ms);

        // Assert
        result.Should().Be("image/png");
    }

    [Fact]
    public void DetectMimeType_UnknownBytes_ReturnsTextPlain()
    {
        // Arrange
        var bytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
        using var ms = new MemoryStream(bytes);

        // Act
        var result = _sut.DetectMimeType(ms);

        // Assert
        result.Should().Be("text/plain");
    }

    [Fact]
    public void IsAllowed_Pdf_ReturnsTrue()
    {
        _sut.IsAllowed("application/pdf").Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_UnknownType_ReturnsFalse()
    {
        _sut.IsAllowed("application/x-executable").Should().BeFalse();
    }

    [Fact]
    public void DetectMimeType_ResetsStreamPosition_AfterDetection()
    {
        // Arrange
        var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        using var ms = new MemoryStream(bytes);

        // Act
        _ = _sut.DetectMimeType(ms);

        // Assert: stream position should be reset to 0
        ms.Position.Should().Be(0);
    }
}
