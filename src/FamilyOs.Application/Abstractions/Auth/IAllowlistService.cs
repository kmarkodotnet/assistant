namespace FamilyOs.Application.Abstractions.Auth;

public interface IAllowlistService
{
    bool IsEmailAllowed(string email);
}
