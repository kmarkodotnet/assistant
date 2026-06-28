using FamilyOs.Application.Common.Errors;
using FamilyOs.Infrastructure.Recurrence;

namespace FamilyOs.Infrastructure.Tests.Recurrence;

public sealed class IcalRecurrenceEvaluatorTests
{
    [Fact]
    public void GetNextOccurrence_MonthlyRule_ReturnsNextMonth()
    {
        var lastFired = new DateTime(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc);
        const string rrule = "FREQ=MONTHLY;BYMONTHDAY=5";

        var next = IcalRecurrenceEvaluator.GetNextOccurrence(rrule, lastFired);

        Assert.NotNull(next);
        Assert.Equal(8, next!.Value.Month);
        Assert.Equal(5, next.Value.Day);
    }

    [Fact]
    public void GetNextOccurrence_WeeklyRule_ReturnsNextWeek()
    {
        var lastFired = new DateTime(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc); // Monday
        const string rrule = "FREQ=WEEKLY;BYDAY=MO";

        var next = IcalRecurrenceEvaluator.GetNextOccurrence(rrule, lastFired);

        Assert.NotNull(next);
        Assert.True((next!.Value - lastFired).TotalDays >= 6);
        Assert.True((next.Value - lastFired).TotalDays <= 8);
    }

    [Fact]
    public void GetNextOccurrence_AnnualRule_ReturnsNextYear()
    {
        var lastFired = new DateTime(2026, 6, 28, 9, 0, 0, DateTimeKind.Utc);
        const string rrule = "FREQ=YEARLY;BYMONTH=6;BYMONTHDAY=28";

        var next = IcalRecurrenceEvaluator.GetNextOccurrence(rrule, lastFired);

        Assert.NotNull(next);
        Assert.Equal(2027, next!.Value.Year);
        Assert.Equal(6, next.Value.Month);
        Assert.Equal(28, next.Value.Day);
    }

    [Fact]
    public void GetNextOccurrence_InvalidRule_ThrowsDomainException()
    {
        var lastFired = DateTime.UtcNow;
        const string invalidRule = "NOT_A_VALID_RRULE";

        Assert.Throws<DomainBusinessRuleException>(() =>
            IcalRecurrenceEvaluator.GetNextOccurrence(invalidRule, lastFired));
    }

    [Fact]
    public void GetNextOccurrence_DailyRule_ReturnsTomorrow()
    {
        var lastFired = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        const string rrule = "FREQ=DAILY";

        var next = IcalRecurrenceEvaluator.GetNextOccurrence(rrule, lastFired);

        Assert.NotNull(next);
        Assert.Equal(2, next!.Value.Day);
        Assert.Equal(7, next.Value.Month);
    }
}
