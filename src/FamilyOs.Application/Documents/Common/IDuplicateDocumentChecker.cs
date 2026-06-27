using FamilyOs.Domain.Entities;

namespace FamilyOs.Application.Documents.Common;

public interface IDuplicateDocumentChecker
{
    Task<Document?> FindDuplicateAsync(string sha256, CancellationToken ct = default);
}
