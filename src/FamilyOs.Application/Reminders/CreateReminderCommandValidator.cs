using FluentValidation;

namespace FamilyOs.Application.Reminders;

public sealed class CreateReminderCommandValidator : AbstractValidator<CreateReminderCommand>
{
    public CreateReminderCommandValidator()
    {
        RuleFor(x => x.TargetUserAccountId).NotEmpty().WithMessage("A célfelhasználó kötelező.");
        RuleFor(x => x.TriggerUtc).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Az emlékeztető időpontja nem lehet a múltban.");

        // At most one anchor: TaskId, DeadlineId, or neither (standalone). Never both. (ADR-0011 D5)
        RuleFor(x => x)
            .Must(x => !(x.TaskId.HasValue && x.DeadlineId.HasValue))
            .WithName("Target")
            .WithMessage("Egyszerre nem adható meg TaskId és DeadlineId; horgony nélküli emlékeztetőnél mindkettő elhagyható.");
    }
}
