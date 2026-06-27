using MediatR;

namespace FamilyOs.Application.Documents.DownloadDocument;

public record DownloadDocumentQuery(Guid DocumentId) : IRequest<DownloadDocumentResult>;

public record DownloadDocumentResult(Stream Stream, string MimeType, string FileName);
