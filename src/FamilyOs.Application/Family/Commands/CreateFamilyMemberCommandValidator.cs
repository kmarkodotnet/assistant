using FluentValidation;

namespace FamilyOs.Application.Family.Commands;

public sealed class CreateFamilyMemberCommandValidator : AbstractValidator<CreateFamilyMemberCommand>
{
    public CreateFamilyMemberCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("A megjelenítési név megadása kötelező.")
            .MaximumLength(200).WithMessage("A megjelenítési név legfeljebb 200 karakter lehet.");
    }
}
