namespace FamilyOs.Application.Common.Errors;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object id)
        : base(
            $"{entityName} with id '{id}' was not found.",
            $"A keresett {entityName} nem található.")
    {
    }

    public NotFoundException(string userMessage)
        : base("Resource not found.", userMessage)
    {
    }
}
