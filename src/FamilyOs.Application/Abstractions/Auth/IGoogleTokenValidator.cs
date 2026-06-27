namespace FamilyOs.Application.Abstractions.Auth;

public sealed record GoogleClaimsResult(string Email, string GoogleSubject, string Name);

public interface IGoogleTokenValidator
{
    Task<GoogleClaimsResult> ValidateAsync(string idToken);
}
