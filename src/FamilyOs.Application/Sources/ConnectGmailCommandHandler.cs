using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FamilyOs.Application.Sources;

public sealed class ConnectGmailCommandHandler(IFamilyOsDbContext db) : IRequestHandler<ConnectGmailCommand>
{
    public async Task Handle(ConnectGmailCommand request, CancellationToken cancellationToken)
    {
        var configJson = JsonSerializer.Serialize(new { refresh_token = request.RefreshToken });

        var existing = await db.Sources
            .FirstOrDefaultAsync(s => s.Kind == SourceKind.GmailAccount, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateConfig(configJson);
        }
        else
        {
            db.Sources.Add(Source.Create("Gmail", SourceKind.GmailAccount, configJson));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
