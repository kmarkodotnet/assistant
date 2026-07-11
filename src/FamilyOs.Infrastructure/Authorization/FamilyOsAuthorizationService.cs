using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Common.Authorization;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;

namespace FamilyOs.Infrastructure.Authorization;

public sealed class FamilyOsAuthorizationService(ICurrentUserAccessor currentUser) : IFamilyOsAuthorizationService
{
    public bool CanReadDocument(Document document)
    {
        var role = currentUser.Role;
        if (role == nameof(UserRole.Admin)) return true;

        if (role == nameof(UserRole.Adult))
        {
            // Own document
            if (document.CreatedByUserAccountId == currentUser.UserAccountId) return true;
            // Other's document: readable only if not private
            return !document.IsPrivate;
        }

        if (role == nameof(UserRole.Child))
        {
            // Child: only related family member docs that are not private
            return document.RelatedFamilyMemberId == currentUser.FamilyMemberId && !document.IsPrivate;
        }

        return false;
    }

    public bool CanWriteDocument(Document document)
    {
        var role = currentUser.Role;
        if (role == nameof(UserRole.Admin)) return true;

        if (role == nameof(UserRole.Adult))
        {
            // Adult can write own documents only
            return document.CreatedByUserAccountId == currentUser.UserAccountId;
        }

        // Child cannot write documents
        return false;
    }

    public bool CanReadMedicalRecord(MedicalRecord record)
    {
        var role = currentUser.Role;
        if (role == nameof(UserRole.Admin)) return true;

        // The affected family member's user can read
        if (record.FamilyMemberId == currentUser.FamilyMemberId) return true;

        return false;
    }

    // Mirrors CanReadDocument's Admin/Adult/Child shape — added for the create_reminder tool
    // resolve step (ai-pipeline.md §11.2), which must not leak another user's private
    // task/deadline title+date into a confirmation card before the user even confirms
    // (code review finding on c43dd87).
    public bool CanReadTask(FamilyTask task)
    {
        var role = currentUser.Role;
        if (role == nameof(UserRole.Admin)) return true;

        if (role == nameof(UserRole.Adult))
        {
            if (task.CreatedByUserAccountId == currentUser.UserAccountId) return true;
            return !task.IsPrivate;
        }

        if (role == nameof(UserRole.Child))
            return task.AssignedToFamilyMemberId == currentUser.FamilyMemberId && !task.IsPrivate;

        return false;
    }

    public bool CanReadDeadline(Deadline deadline)
    {
        var role = currentUser.Role;
        if (role == nameof(UserRole.Admin)) return true;

        if (role == nameof(UserRole.Adult))
        {
            if (deadline.CreatedByUserAccountId == currentUser.UserAccountId) return true;
            return !deadline.IsPrivate;
        }

        if (role == nameof(UserRole.Child))
            return deadline.RelatedFamilyMemberId == currentUser.FamilyMemberId && !deadline.IsPrivate;

        return false;
    }
}
