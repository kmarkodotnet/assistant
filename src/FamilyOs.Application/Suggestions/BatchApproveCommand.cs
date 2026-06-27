using MediatR;

namespace FamilyOs.Application.Suggestions;

public sealed record BatchApproveItem(string EntityType, Guid Id, string Action);

public sealed record BatchApproveCommand(
    IReadOnlyList<BatchApproveItem> Items,
    Guid ApprovedByUserId
) : IRequest<BatchApproveResult>;

public sealed class BatchApproveResult
{
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public List<string> Errors { get; set; } = [];
}
