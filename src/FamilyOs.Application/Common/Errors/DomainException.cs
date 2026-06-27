namespace FamilyOs.Application.Common.Errors;

/// <summary>
/// Base for all domain exceptions. Message (English) goes to logs; UserMessage (Hungarian) goes to HTTP response.
/// </summary>
public abstract class DomainException : Exception
{
    public string UserMessage { get; }

    protected DomainException(string message, string userMessage, Exception? inner = null)
        : base(message, inner)
    {
        UserMessage = userMessage;
    }
}
