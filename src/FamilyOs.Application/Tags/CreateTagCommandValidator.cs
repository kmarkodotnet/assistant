using FluentValidation;

namespace FamilyOs.Application.Tags;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(40)
            .Matches(@"^[a-záéíóöőúüű\w\s\-]+$")
            .WithMessage("A tag neve kötelező, 1-40 karakter, csak betűk, számok, szóköz és kötőjel.");

        RuleFor(x => x.Color)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .When(x => x.Color is not null)
            .WithMessage("A szín formátuma: #RRGGBB.");
    }
}
