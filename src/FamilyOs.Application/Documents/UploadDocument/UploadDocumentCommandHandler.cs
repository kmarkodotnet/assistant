using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Abstractions.Storage;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Application.Documents.Common;
using FamilyOs.Application.Documents.Dtos;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using System.Security.Cryptography;
using FamilyOs.Application.Common.Ai;

namespace FamilyOs.Application.Documents.UploadDocument;

public sealed class UploadDocumentCommandHandler(
    IFamilyOsDbContext db,
    IDocumentStorage storage,
    IMimeDetector mimeDetector,
    IDuplicateDocumentChecker duplicateChecker,
    ICurrentUserAccessor currentUser,
    IAuditLogger auditLogger)
    : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(UploadDocumentCommand cmd, CancellationToken cancellationToken)
    {
        // 1. MIME detection
        var mimeType = mimeDetector.DetectMimeType(cmd.FileStream);
        if (!mimeDetector.IsAllowed(mimeType))
            throw new UnsupportedMediaException($"A '{mimeType}' fájltípus nem támogatott.");

        // 2. SHA256 hash
        cmd.FileStream.Position = 0;
        var sha256Bytes = await Task.Run(() => SHA256.HashData(cmd.FileStream), cancellationToken);
        var sha256 = Convert.ToHexString(sha256Bytes).ToLowerInvariant();
        cmd.FileStream.Position = 0;

        // 3. Dedup check
        var existing = await duplicateChecker.FindDuplicateAsync(sha256, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"Ez a fájl már fel van töltve. Dokumentum azonosítója: {existing.Id}");

        // 4. Storage save
        var storagePath = await storage.SaveAsync(cmd.FileStream, cmd.OriginalFileName, mimeType, cancellationToken);

        // 5. DB insert
        var userAccountId = currentUser.UserAccountId ?? throw new ForbiddenException("Bejelentkezés szükséges.");
        var title = cmd.Title ?? Path.GetFileNameWithoutExtension(cmd.OriginalFileName);
        cmd.FileStream.Position = 0;
        var sizeBytes = cmd.FileStream.Length;

        var doc = Document.Create(
            title: title,
            originalFileName: cmd.OriginalFileName,
            mimeType: mimeType,
            sizeBytes: sizeBytes,
            storagePath: storagePath,
            sha256: sha256,
            sourceType: SourceType.Upload,
            origin: Origin.Manual,
            createdByUserAccountId: userAccountId,
            documentDate: cmd.DocumentDate,
            relatedFamilyMemberId: cmd.RelatedFamilyMemberId,
            isPrivate: cmd.IsPrivate
        );

        db.Documents.Add(doc);
        db.AiProcessingJobs.Add(AiProcessingJob.Create(AiJobType.ExtractText, doc.Id));
        await db.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditAction.Create,
            userAccountId,
            entityType: "Document",
            entityId: doc.Id,
            ct: cancellationToken);

        return new DocumentDto(
            doc.Id,
            doc.Title,
            doc.OriginalFileName,
            doc.MimeType,
            doc.SizeBytes,
            doc.Sha256,
            doc.SourceType,
            doc.IsPrivate,
            doc.ProcessingStatus,
            doc.DocumentDate,
            doc.RelatedFamilyMemberId,
            doc.CreatedByUserAccountId,
            doc.CreatedUtc,
            doc.UpdatedUtc);
    }
}
