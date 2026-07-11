namespace FamilyOs.Application.Common.Errors;

/// <summary>
/// Maps to 401. Added for tool-call proposal tokens (ADR-0011 D1): expired or
/// signature-invalid tokens are an authentication-shaped failure, distinct from
/// ForbiddenException (403, used when the token/entity belongs to someone else).
/// </summary>
public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException(string userMessage)
        : base($"Unauthorized: {userMessage}", userMessage)
    {
    }
}
