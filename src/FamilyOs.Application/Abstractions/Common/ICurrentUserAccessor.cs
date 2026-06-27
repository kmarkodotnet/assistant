namespace FamilyOs.Application.Abstractions.Common;

public interface ICurrentUserAccessor
{
    Guid? UserAccountId { get; }
    Guid? FamilyMemberId { get; }
    string? Role { get; }
}
