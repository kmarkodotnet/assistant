namespace FamilyOs.Application.Common.Errors;

public sealed class DomainBusinessRuleException : DomainException
{
    public DomainBusinessRuleException(string userMessage)
        : base($"Business rule violation: {userMessage}", userMessage)
    {
    }
}
