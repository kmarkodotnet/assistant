using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;

namespace FamilyOs.Workers.Services;

/// <summary>
/// RBAC visibility filters for the daily digest (contract §3 / ADR-0007).
/// Expressed as <see cref="IQueryable{T}"/> extensions so the exact same
/// predicate translates to SQL against EF Core and can also be exercised
/// directly against in-memory collections (<c>List&lt;T&gt;.AsQueryable()</c>) in
/// unit tests, without needing a database.
/// </summary>
public static class DailyDigestRbac
{
    /// <summary>
    /// Admin/Adult: family-wide non-private records, plus their own private ones.
    /// Child: only records tied to them, and never private ones.
    /// </summary>
    public static IQueryable<Deadline> VisibleTo(this IQueryable<Deadline> query, UserAccount user)
        => user.Role == UserRole.Child
            ? query.Where(d => !d.IsPrivate && d.RelatedFamilyMemberId == user.FamilyMemberId)
            : query.Where(d => !d.IsPrivate || d.CreatedByUserAccountId == user.Id);

    public static IQueryable<Document> VisibleTo(this IQueryable<Document> query, UserAccount user)
        => user.Role == UserRole.Child
            ? query.Where(d => !d.IsPrivate && d.RelatedFamilyMemberId == user.FamilyMemberId)
            : query.Where(d => !d.IsPrivate || d.CreatedByUserAccountId == user.Id);
}
