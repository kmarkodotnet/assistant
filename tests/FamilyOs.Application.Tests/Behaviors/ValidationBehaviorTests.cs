using FamilyOs.Application.Common.Behaviors;
using FamilyOs.Application.Common.Errors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;

namespace FamilyOs.Application.Tests.Behaviors;

public record TestBehaviorRequest(string Value) : IRequest<string>;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestBehaviorRequest, string>([]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("ok");

        // Act
        var result = await behavior.Handle(new TestBehaviorRequest("x"), next, default);

        // Assert
        result.Should().Be("ok");
        await next.Received(1)();
    }

    [Fact]
    public async Task Handle_WhenValidatorPasses_CallsNext()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestBehaviorRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestBehaviorRequest>>(), default)
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestBehaviorRequest, string>([validator]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("ok");

        // Act
        var result = await behavior.Handle(new TestBehaviorRequest("x"), next, default);

        // Assert
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenValidatorFails_ThrowsValidationException()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Value", "Kötelező mező."),
        };
        var validator = Substitute.For<IValidator<TestBehaviorRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestBehaviorRequest>>(), default)
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestBehaviorRequest, string>([validator]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act
        var act = async () => await behavior.Handle(new TestBehaviorRequest(""), next, default);

        // Assert
        await act.Should().ThrowAsync<FamilyOs.Application.Common.Errors.ValidationException>()
            .Where(e => e.Errors.ContainsKey("Value"));
    }
}
