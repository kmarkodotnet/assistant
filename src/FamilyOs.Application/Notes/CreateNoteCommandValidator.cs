using FluentValidation;

namespace FamilyOs.Application.Notes;

public sealed class CreateNoteCommandValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500).WithMessage("A cím kötelező és maximum 500 karakter lehet.");
        RuleFor(x => x.Body).NotEmpty().WithMessage("A tartalom kötelező.");
        RuleFor(x => x.CreatedByUserId).NotEmpty().WithMessage("A létrehozó felhasználó kötelező.");
    }
}
