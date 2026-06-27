namespace FamilyOs.Application.Common.Errors;

/// <summary>
/// Base for infrastructure-level exceptions. Never expose internal details to end users.
/// </summary>
public abstract class InfrastructureException : Exception
{
    protected InfrastructureException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

public sealed class AiProviderUnavailableException : InfrastructureException
{
    public AiProviderUnavailableException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

public sealed class ExternalServiceException : InfrastructureException
{
    public ExternalServiceException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

public sealed class StorageException : InfrastructureException
{
    public StorageException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
