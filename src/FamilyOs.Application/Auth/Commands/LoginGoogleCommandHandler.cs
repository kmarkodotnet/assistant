using FamilyOs.Application.Abstractions.Auth;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Auth.Dtos;
using FamilyOs.Application.Auth.Options;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyOs.Application.Auth.Commands;

public sealed class LoginGoogleCommandHandler(
    IGoogleTokenValidator tokenValidator,
    IAllowlistService allowlistService,
    IFamilyOsDbContext db,
    IOptions<AuthOptions> authOptions)
    : IRequestHandler<LoginGoogleCommand, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(
        LoginGoogleCommand request,
        CancellationToken cancellationToken)
    {
        var claims = await tokenValidator.ValidateAsync(request.IdToken);

        if (!allowlistService.IsEmailAllowed(claims.Email))
            throw new ForbiddenException("A megadott e-mail cím nincs az engedélyezési listán.");

        var existing = await db.UserAccounts
            .Include(u => u.FamilyMember)
            .FirstOrDefaultAsync(u => u.GoogleSubject == claims.GoogleSubject, cancellationToken);

        if (existing is not null)
        {
            existing.RecordLogin();
            await db.SaveChangesAsync(cancellationToken);
            return MapToDto(existing);
        }

        // Check for pending invite
        var invite = await db.PendingInvites
            .FirstOrDefaultAsync(i => i.Email == claims.Email.ToLowerInvariant().Trim(), cancellationToken);

        FamilyMember familyMember;
        UserRole role;

        if (invite is not null)
        {
            var fm = await db.FamilyMembers
                .FirstOrDefaultAsync(f => f.Id == invite.FamilyMemberId, cancellationToken);
            familyMember = fm ?? throw new NotFoundException("FamilyMember", invite.FamilyMemberId);
            role = Enum.TryParse<UserRole>(invite.Role, out var parsedRole) ? parsedRole : UserRole.Child;
            db.PendingInvites.Remove(invite);
        }
        else
        {
            // Check if this is the bootstrap admin
            var bootstrapAdmin = authOptions.Value.BootstrapAdmin;
            var isBootstrap = !string.IsNullOrWhiteSpace(bootstrapAdmin)
                && string.Equals(bootstrapAdmin.Trim(), claims.Email.Trim(), StringComparison.OrdinalIgnoreCase);

            role = isBootstrap ? UserRole.Admin : UserRole.Child;

            familyMember = FamilyMember.Create(
                displayName: claims.Name,
                relation: Relation.Self);
            db.FamilyMembers.Add(familyMember);
        }

        var account = UserAccount.Create(
            familyMemberId: familyMember.Id,
            googleSubject: claims.GoogleSubject,
            email: claims.Email,
            displayName: claims.Name,
            role: role);

        account.RecordLogin();
        familyMember.MarkHasUserAccount(true);
        db.UserAccounts.Add(account);

        await db.SaveChangesAsync(cancellationToken);

        return MapToDto(account);
    }

    private static CurrentUserDto MapToDto(UserAccount account)
        => new(
            UserAccountId: account.Id,
            FamilyMemberId: account.FamilyMemberId,
            DisplayName: account.DisplayName,
            Email: account.Email,
            Role: account.Role.ToString(),
            Preferences: new UserPreferencesDto(
                EmailEnabled: account.EmailEnabled,
                QuietHoursStart: account.QuietHoursStart,
                QuietHoursEnd: account.QuietHoursEnd));
}
