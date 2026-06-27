using FamilyOs.Application.Abstractions.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace FamilyOs.Infrastructure.Auth;

public sealed class GoogleTokenValidator(IConfiguration configuration) : IGoogleTokenValidator
{
    public async Task<GoogleClaimsResult> ValidateAsync(string idToken)
    {
        var clientId = configuration["Auth:GoogleClientId"]
            ?? throw new InvalidOperationException("Auth:GoogleClientId is not configured.");

        var payload = await GoogleJsonWebSignature.ValidateAsync(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [clientId],
            });

        return new GoogleClaimsResult(
            Email: payload.Email,
            GoogleSubject: payload.Subject,
            Name: payload.Name ?? payload.Email);
    }
}
