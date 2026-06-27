using FamilyOs.Application.Abstractions.Storage;
using Microsoft.Extensions.Configuration;

namespace FamilyOs.Infrastructure.Storage;

public sealed class LocalFilesystemDocumentStorage(IConfiguration configuration) : IDocumentStorage
{
    private readonly string _baseDir = configuration["Storage:BasePath"] is { Length: > 0 } p
        ? p
        : Path.Combine(AppContext.BaseDirectory, "data", "documents");

    public async Task<string> SaveAsync(Stream content, string originalFileName, string mimeType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        var now = DateTime.UtcNow;
        var relativePath = Path.Combine(now.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), now.Month.ToString("D2", System.Globalization.CultureInfo.InvariantCulture), $"{Guid.NewGuid()}{ext}");
        var fullPath = Path.Combine(_baseDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, ct);
        return relativePath;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        ValidatePath(storagePath);
        var fullPath = Path.GetFullPath(Path.Combine(_baseDir, storagePath));
        if (!fullPath.StartsWith(_baseDir, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path traversal detected.");
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        ValidatePath(storagePath);
        var fullPath = Path.GetFullPath(Path.Combine(_baseDir, storagePath));
        if (!fullPath.StartsWith(_baseDir, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path traversal detected.");
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private static void ValidatePath(string path)
    {
        if (path.Contains("..") || path.Contains('\0'))
            throw new InvalidOperationException("Invalid storage path.");
    }
}
