using FamilyOs.Application.Abstractions.Auth;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Auth.Commands;
using FamilyOs.Application.Auth.Options;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FamilyOs.Application.Tests.Auth;

public sealed class LoginGoogleCommandHandlerTests
{
    private readonly IGoogleTokenValidator _tokenValidator = Substitute.For<IGoogleTokenValidator>();
    private readonly IAllowlistService _allowlistService = Substitute.For<IAllowlistService>();
    private readonly IOptions<AuthOptions> _options = Options.Create(new AuthOptions
    {
        BootstrapAdmin = "admin@test.com",
        GoogleClientId = "test-client-id",
    });

    [Fact]
    public async Task Handle_WhenEmailNotAllowed_ThrowsForbiddenException()
    {
        // Arrange
        _tokenValidator.ValidateAsync("token").Returns(new GoogleClaimsResult(
            "blocked@user.com", "sub123", "Blocked User"));
        _allowlistService.IsEmailAllowed("blocked@user.com").Returns(false);

        var db = Substitute.For<IFamilyOsDbContext>();
        var handler = new LoginGoogleCommandHandler(
            _tokenValidator, _allowlistService, db, _options);

        // Act
        var act = async () => await handler.Handle(new LoginGoogleCommand("token"), default);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenTokenValidatorThrows_PropagatesException()
    {
        // Arrange
        _tokenValidator.ValidateAsync("bad-token")
            .ThrowsAsync(new ForbiddenException("Érvénytelen azonosítási token."));

        var db = Substitute.For<IFamilyOsDbContext>();
        var handler = new LoginGoogleCommandHandler(
            _tokenValidator, _allowlistService, db, _options);

        // Act
        var act = async () => await handler.Handle(new LoginGoogleCommand("bad-token"), default);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
