using MediatR;

namespace FamilyOs.Application.Audit;

public sealed record ExportAuditLogQuery(DateTime? From, DateTime? To, string Format) : IRequest<AuditExportResult>;

public sealed record AuditExportResult(string ContentType, string FileName, IAsyncEnumerable<string> Lines);
