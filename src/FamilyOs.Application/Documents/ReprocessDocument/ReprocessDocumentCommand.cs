using MediatR;

namespace FamilyOs.Application.Documents.ReprocessDocument;

public record ReprocessDocumentCommand(
    Guid DocumentId,
    IReadOnlyList<string> Jobs
) : IRequest<ReprocessResult>;

public record ReprocessResult(IReadOnlyList<Guid> QueuedJobIds);
