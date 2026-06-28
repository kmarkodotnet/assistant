using FluentValidation;

namespace FamilyOs.Application.Topics;

public sealed class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("A téma neve kötelező, maximum 200 karakter.");
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(200)
            .Matches(@"^[a-z0-9\-]+$")
            .WithMessage("A slug csak kisbetűket, számokat és kötőjelet tartalmazhat.");
    }
}
