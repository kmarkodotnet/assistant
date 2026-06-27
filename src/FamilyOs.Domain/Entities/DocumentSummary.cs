namespace FamilyOs.Domain.Entities;

public sealed class DocumentSummary
{
    private DocumentSummary() { }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = string.Empty;
    public string PromptVersion { get; private set; } = string.Empty;
    public bool IsCurrent { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public Document? Document { get; private set; }

    public static DocumentSummary Create(Guid documentId, string content, string modelName, string promptVersion)
        => new()
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Content = content,
            ModelName = modelName,
            PromptVersion = promptVersion,
            IsCurrent = true,
            CreatedUtc = DateTime.UtcNow,
        };

    public void Supersede() { IsCurrent = false; }
}
