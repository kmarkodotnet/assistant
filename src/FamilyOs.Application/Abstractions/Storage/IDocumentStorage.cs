namespace FamilyOs.Application.Abstractions.Storage;

public interface IDocumentStorage
{
    Task<string> SaveAsync(Stream content, string originalFileName, string mimeType, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
