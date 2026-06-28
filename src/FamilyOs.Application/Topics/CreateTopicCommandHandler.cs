using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Topics.Dtos;
using FamilyOs.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Topics;

public sealed class CreateTopicCommandHandler : IRequestHandler<CreateTopicCommand, TopicDto>
{
    private readonly IFamilyOsDbContext _db;

    public CreateTopicCommandHandler(IFamilyOsDbContext db) => _db = db;

    public async Task<TopicDto> Handle(CreateTopicCommand request, CancellationToken cancellationToken)
    {
        var normalizedSlug = request.Slug.ToLowerInvariant();
        var slugExists = await _db.Topics
            .AnyAsync(t => t.Slug == normalizedSlug, cancellationToken);

        if (slugExists)
            throw new InvalidOperationException($"A '{request.Slug}' slug már foglalt.");

        if (request.ParentId.HasValue)
        {
            var parent = await _db.Topics.FindAsync([request.ParentId.Value], cancellationToken)
                ?? throw new KeyNotFoundException($"Szülő téma {request.ParentId} nem található.");

            if (parent.ParentId.HasValue)
            {
                var grandParentExists = await _db.Topics.AnyAsync(t => t.Id == parent.ParentId, cancellationToken);
                var grandParent = await _db.Topics.FindAsync([parent.ParentId.Value], cancellationToken);
                if (grandParent is not null && grandParent.ParentId.HasValue)
                    throw new InvalidOperationException("A témák maximum 3 szintben ágyazhatók egymásba.");
            }
        }

        var topic = Topic.Create(request.Name, request.Slug, request.ParentId, request.Icon, request.SortOrder);
        _db.Topics.Add(topic);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(topic);
    }

    internal static TopicDto MapToDto(Topic t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Slug = t.Slug,
        ParentId = t.ParentId,
        Icon = t.Icon,
        SortOrder = t.SortOrder,
        CreatedUtc = t.CreatedUtc,
    };
}
