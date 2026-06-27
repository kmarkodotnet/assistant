namespace FamilyOs.Domain.Enums;

// ExternalApiCall: Gmail API, SMTP és egyéb külső szolgáltatás-hívások (v0.2 spec)
public enum AuditAction
{
    Create, Update, Delete, Login, LoginFailed,
    Approve, Reject, AiCall, FileAccess, PermissionChange, ExternalApiCall,
}
