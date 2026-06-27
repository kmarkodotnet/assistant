using FluentValidation;

namespace FamilyOs.Application.Reminders;

public sealed class CreateReminderCommandValidator : AbstractValidator<CreateReminderCommand>
{
    public CreateReminderCommandValidator()
    {
        RuleFor(x => x.TargetUserAccountId).NotEmpty().WithMessage("A célfelhasználó kötelező.");
        RuleFor(x => x.TriggerUtc).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Az emlékeztető időpontja nem lehet a múltban.");

        // XOR: exactly one of TaskId / DeadlineId must be set
        RuleFor(x => x)
            .Must(x => (x.TaskId.HasValue && !x.DeadlineId.HasValue) || (!x.TaskId.HasValue && x.DeadlineId.HasValue))
            .WithName("Target")
            .WithMessage("Pontosan egy célentitást kell megadni: vagy TaskId vagy DeadlineId.");
    }
}
