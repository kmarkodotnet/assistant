using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = FamilyOs.Domain.Enums.TaskStatus;
using DomainDeadlineStatus = FamilyOs.Domain.Enums.DeadlineStatus;

namespace FamilyOs.Application.Suggestions;

public sealed class BatchApproveCommandHandler : IRequestHandler<BatchApproveCommand, BatchApproveResult>
{
    private readonly IFamilyOsDbContext _db;

    public BatchApproveCommandHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<BatchApproveResult> Handle(
        BatchApproveCommand request,
        CancellationToken cancellationToken)
    {
        var result = new BatchApproveResult();

        foreach (var item in request.Items)
        {
            try
            {
                switch (item.EntityType.ToLowerInvariant())
                {
                    case "task":
                        await ProcessTaskAsync(item, request.ApprovedByUserId, cancellationToken);
                        break;
                    case "deadline":
                        await ProcessDeadlineAsync(item, request.ApprovedByUserId, cancellationToken);
                        break;
                    case "tag":
                        await ProcessTagAsync(item, cancellationToken);
                        break;
                    case "topic":
                        await ProcessTopicAsync(item, cancellationToken);
                        break;
                    default:
                        result.Errors.Add($"Ismeretlen entityType: {item.EntityType} (id={item.Id})");
                        continue;
                }

                if (item.Action.Equals("approve", StringComparison.OrdinalIgnoreCase))
                    result.Approved++;
                else
                    result.Rejected++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{item.EntityType}/{item.Id}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task ProcessTaskAsync(BatchApproveItem item, Guid approvedByUserId, CancellationToken ct)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == item.Id, ct)
            ?? throw new InvalidOperationException($"Feladat nem található: {item.Id}");

        if (item.Action.Equals("approve", StringComparison.OrdinalIgnoreCase))
        {
            TaskStateMachine.Transition(task, DomainTaskStatus.Open);
            task.Approve(approvedByUserId);
        }
        else
        {
            task.Reject();
        }
    }

    private async Task ProcessDeadlineAsync(BatchApproveItem item, Guid approvedByUserId, CancellationToken ct)
    {
        var deadline = await _db.Deadlines
            .FirstOrDefaultAsync(d => d.Id == item.Id, ct)
            ?? throw new InvalidOperationException($"Határidő nem található: {item.Id}");

        if (item.Action.Equals("approve", StringComparison.OrdinalIgnoreCase))
        {
            deadline.Approve(approvedByUserId);
        }
        else
        {
            DeadlineStateMachine.Transition(deadline, DomainDeadlineStatus.Dismissed);
        }
    }

    private async Task ProcessTagAsync(BatchApproveItem item, CancellationToken ct)
    {
        var tagId = item.SecondaryId ?? throw new InvalidOperationException("TagId hiányzik");
        var docTag = await _db.DocumentTags
            .FirstOrDefaultAsync(dt => dt.DocumentId == item.Id && dt.TagId == tagId, ct)
            ?? throw new InvalidOperationException($"DocumentTag nem található: doc={item.Id}, tag={tagId}");

        if (item.Action.Equals("approve", StringComparison.OrdinalIgnoreCase))
            docTag.IsApproved = true;
        else
            _db.DocumentTags.Remove(docTag);
    }

    private async Task ProcessTopicAsync(BatchApproveItem item, CancellationToken ct)
    {
        var topicId = item.SecondaryId ?? throw new InvalidOperationException("TopicId hiányzik");
        var docTopic = await _db.DocumentTopics
            .FirstOrDefaultAsync(dt => dt.DocumentId == item.Id && dt.TopicId == topicId, ct)
            ?? throw new InvalidOperationException($"DocumentTopic nem található: doc={item.Id}, topic={topicId}");

        if (item.Action.Equals("approve", StringComparison.OrdinalIgnoreCase))
            docTopic.IsApproved = true;
        else
            _db.DocumentTopics.Remove(docTopic);
    }
}
