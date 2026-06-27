namespace FamilyOs.Domain.Services;

public static class ReminderTriggerCalculator
{
    public static DateTime Calculate(DateTime dueDateUtc, int offsetMinutesBeforeDue)
        => dueDateUtc.AddMinutes(-offsetMinutesBeforeDue);

    public static DateTime CalculateFromOffsetDays(DateTime dueDateUtc, int offsetDays)
        => dueDateUtc.AddDays(-offsetDays);
}
