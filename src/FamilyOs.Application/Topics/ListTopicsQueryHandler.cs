using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Topics.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Topics;

public sealed class ListTopicsQueryHandler : IRequestHandler<ListTopicsQuery, List<TopicDto>>
{
    private readonly IFamilyOsDbContext _db;

    public ListTopicsQueryHandler(IFamilyOsDbContext db) => _db = db;

    public async Task<List<TopicDto>> Handle(ListTopicsQuery request, CancellationToken cancellationToken)
    {
        var all = await _db.Topics
            .AsNoTracking()
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .Select(t => new TopicDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                ParentId = t.ParentId,
                Icon = t.Icon,
                SortOrder = t.SortOrder,
                CreatedUtc = t.CreatedUtc,
            })
            .ToListAsync(cancellationToken);

        if (request.Flat) return all;

        return BuildTree(all, null);
    }

    private static List<TopicDto> BuildTree(List<TopicDto> all, Guid? parentId)
    {
        return all
            .Where(t => t.ParentId == parentId)
            .Select(t =>
            {
                t.Children = BuildTree(all, t.Id);
                return t;
            })
            .ToList();
    }
}
