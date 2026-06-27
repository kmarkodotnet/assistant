using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Application.Deadlines.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Deadlines;

public sealed class GetDeadlineQueryHandler : IRequestHandler<GetDeadlineQuery, DeadlineDto>
{
    private readonly IFamilyOsDbContext _db;

    public GetDeadlineQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<DeadlineDto> Handle(GetDeadlineQuery request, CancellationToken cancellationToken)
    {
        var deadline = await _db.Deadlines
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DeadlineId, cancellationToken)
            ?? throw new NotFoundException("Deadline", request.DeadlineId);

        if (deadline.IsPrivate && deadline.CreatedByUserAccountId != request.UserId)
            throw new ForbiddenException("Nincs jogosultsága megtekinteni ezt a határidőt.");

        return CreateDeadlineCommandHandler.MapToDto(deadline);
    }
}
