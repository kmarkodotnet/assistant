using FluentValidation;

namespace FamilyOs.Application.Deadlines;

public sealed class CreateDeadlineCommandValidator : AbstractValidator<CreateDeadlineCommand>
{
    public CreateDeadlineCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.DueDateUtc).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("A határidő nem lehet a múltban.");
    }
}
