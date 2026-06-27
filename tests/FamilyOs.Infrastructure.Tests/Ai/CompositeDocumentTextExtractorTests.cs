using System.Text;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Extraction;
using FamilyOs.Infrastructure.Ai.Options;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FamilyOs.Infrastructure.Tests.Ai;

public sealed class CompositeDocumentTextExtractorTests
{
    private static CompositeDocumentTextExtractor CreateSut()
    {
        var tesseractOptions = Options.Create(new TesseractOptions
        {
            DataPath = "/tessdata-not-real",
            Languages = "hun+eng",
        });

        var pdfExtractor = new PdfTextLayerExtractor();
        var ocrExtractor = new TesseractOcrExtractor(tesseractOptions);
        return new CompositeDocumentTextExtractor(pdfExtractor, ocrExtractor);
    }

    [Fact]
    public async Task ExtractAsync_PlainText_ReturnsTextContent()
    {
        var sut = CreateSut();
        var text = "Hello FamilyOs World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        var result = await sut.ExtractAsync(stream, "text/plain");

        result.Text.Should().Be(text);
        result.ExtractionMethod.Should().Be("PlainText");
    }

    [Fact]
    public async Task ExtractAsync_PlainText_EmptyContent_ReturnsEmptyString()
    {
        var sut = CreateSut();
        using var stream = new MemoryStream(Array.Empty<byte>());

        var result = await sut.ExtractAsync(stream, "text/plain");

        result.Text.Should().BeEmpty();
        result.ExtractionMethod.Should().Be("PlainText");
    }

    [Fact]
    public void CanHandle_PlainText_ReturnsTrue()
    {
        var sut = CreateSut();

        sut.CanHandle("text/plain").Should().BeTrue();
    }

    [Fact]
    public void CanHandle_Pdf_ReturnsTrue()
    {
        var sut = CreateSut();

        sut.CanHandle("application/pdf").Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ImageJpeg_ReturnsTrue()
    {
        var sut = CreateSut();

        sut.CanHandle("image/jpeg").Should().BeTrue();
    }

    [Fact]
    public void CanHandle_Docx_ReturnsTrue()
    {
        var sut = CreateSut();

        sut.CanHandle("application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .Should().BeTrue();
    }

    [Fact]
    public void CanHandle_UnknownMimeType_ReturnsFalse()
    {
        var sut = CreateSut();

        sut.CanHandle("application/octet-stream").Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_InvalidDocx_ReturnsEmptyExtractionResult()
    {
        var sut = CreateSut();
        // Not a valid ZIP/docx — just garbage bytes
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not a zip file"));

        var result = await sut.ExtractAsync(stream,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        result.ExtractionMethod.Should().Be("DocxExtract");
        result.Text.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_InvalidPdf_FallsBackToOcr_AndReturnsEmptyOrText()
    {
        var sut = CreateSut();
        // Not a real PDF — should fail PdfPig and try Tesseract (which also fails gracefully)
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not a pdf"));

        // We just check it doesn't throw
        var act = async () => await sut.ExtractAsync(stream, "application/pdf");
        await act.Should().NotThrowAsync();
    }
}
