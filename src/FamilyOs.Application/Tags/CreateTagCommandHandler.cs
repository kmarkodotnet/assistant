using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Tags.Dtos;
using FamilyOs.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Tags;

public sealed class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, TagDto>
{
    private readonly IFamilyOsDbContext _db;

    public CreateTagCommandHandler(IFamilyOsDbContext db) => _db = db;

    public async Task<TagDto> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.ToLowerInvariant().Trim();

        var existing = await _db.Tags
            .FirstOrDefaultAsync(t => t.Name == normalizedName, cancellationToken);

        if (existing is not null)
            return MapToDto(existing);

        var tag = Tag.Create(request.Name, request.Color);
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync(cancellationToken);
        return MapToDto(tag);
    }

    internal static TagDto MapToDto(Domain.Entities.Tag t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Color = t.Color,
        UsageCount = t.UsageCount,
        CreatedUtc = t.CreatedUtc,
    };
}
