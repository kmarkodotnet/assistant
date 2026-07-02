using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class DocumentText
{
    private DocumentText() { }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string? OriginalContent { get; private set; }
    public ExtractionMethod ExtractionMethod { get; private set; }
    public decimal? OcrConfidence { get; private set; }
    public int CharCount { get; private set; }
    public string? LanguageDetected { get; private set; }
    public bool IsManuallyEdited { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public Document? Document { get; private set; }

    public static DocumentText Create(
        Guid documentId,
        string content,
        ExtractionMethod method,
        string? language = null,
        decimal? ocrConfidence = null)
        => new()
        {
            Id = Guid.CreateVersion7(),
            DocumentId = documentId,
            Content = content,
            ExtractionMethod = method,
            CharCount = content.Length,
            LanguageDetected = language,
            OcrConfidence = ocrConfidence,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

    public void CorrectManually(string newContent)
    {
        if (!IsManuallyEdited) OriginalContent = Content;
        Content = newContent;
        CharCount = newContent.Length;
        IsManuallyEdited = true;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void UpdateContent(string content, ExtractionMethod method, string? language)
    {
        Content = content;
        ExtractionMethod = method;
        CharCount = content.Length;
        LanguageDetected = language;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void SetLanguageDetected(string? language)
    {
        LanguageDetected = language;
        UpdatedUtc = DateTime.UtcNow;
    }
}
