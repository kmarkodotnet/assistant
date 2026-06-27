namespace FamilyOs.Application.Common.Errors;

public sealed class ConflictException : DomainException
{
    public ConflictException(string userMessage)
        : base($"Conflict: {userMessage}", userMessage)
    {
    }
}
