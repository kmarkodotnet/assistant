using FluentValidation;
using System.Text.RegularExpressions;

namespace FamilyOs.Application.Users.Commands;

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    private static readonly Regex TimeRegex = new(@"^\d{2}:\d{2}$", RegexOptions.Compiled);

    public UpdatePreferencesCommandValidator()
    {
        When(x => x.QuietHoursStart is not null, () =>
        {
            RuleFor(x => x.QuietHoursStart)
                .Must(v => v is not null && TimeRegex.IsMatch(v))
                .WithMessage("A csendes időszak kezdete ÓÓ:PP formátumban adható meg.");
        });

        When(x => x.QuietHoursEnd is not null, () =>
        {
            RuleFor(x => x.QuietHoursEnd)
                .Must(v => v is not null && TimeRegex.IsMatch(v))
                .WithMessage("A csendes időszak vége ÓÓ:PP formátumban adható meg.");
        });
    }
}
