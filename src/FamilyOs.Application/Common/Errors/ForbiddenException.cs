namespace FamilyOs.Application.Common.Errors;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string userMessage)
        : base($"Forbidden: {userMessage}", userMessage)
    {
    }
}
