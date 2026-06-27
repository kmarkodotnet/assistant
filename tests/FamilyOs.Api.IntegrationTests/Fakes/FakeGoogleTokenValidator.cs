using FamilyOs.Application.Abstractions.Auth;
using FamilyOs.Application.Common.Errors;

namespace FamilyOs.Api.IntegrationTests.Fakes;

public sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public Task<GoogleClaimsResult> ValidateAsync(string idToken)
    {
        if (idToken == "admin-token")
        {
            return Task.FromResult(new GoogleClaimsResult(
                Email: "admin@test.com",
                GoogleSubject: "google_admin_123",
                Name: "Test Admin"));
        }

        throw new ForbiddenException("Érvénytelen azonosítási token.");
    }
}
