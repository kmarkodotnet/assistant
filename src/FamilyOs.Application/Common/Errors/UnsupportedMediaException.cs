namespace FamilyOs.Application.Common.Errors;

public sealed class UnsupportedMediaException : DomainException
{
    public UnsupportedMediaException(string userMessage)
        : base($"Unsupported media type: {userMessage}", userMessage)
    {
    }
}
