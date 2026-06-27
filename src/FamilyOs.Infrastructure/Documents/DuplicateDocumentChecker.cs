using FamilyOs.Application.Documents.Common;
using FamilyOs.Domain.Entities;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Infrastructure.Documents;

public sealed class DuplicateDocumentChecker(FamilyOsDbContext db) : IDuplicateDocumentChecker
{
    public Task<Document?> FindDuplicateAsync(string sha256, CancellationToken ct)
        => db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Sha256 == sha256, ct);
}
