using FamilyOs.Application.Common.Errors;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace FamilyOs.Infrastructure.Recurrence;

public static class IcalRecurrenceEvaluator
{
    public static DateTime? GetNextOccurrence(string rruleExpression, DateTime lastFiredUtc)
    {
        try
        {
            var calendarEvent = new CalendarEvent
            {
                Start = new CalDateTime(lastFiredUtc),
                RecurrenceRules = [new RecurrencePattern(rruleExpression)],
            };

            var occurrences = calendarEvent.GetOccurrences(
                lastFiredUtc.AddSeconds(1),
                lastFiredUtc.AddYears(2));

            var first = occurrences.FirstOrDefault();
            if (first is null)
                return null;

            return first.Period.StartTime.AsDateTimeOffset.UtcDateTime;
        }
        catch (Exception ex) when (ex is not DomainBusinessRuleException)
        {
            throw new DomainBusinessRuleException("Érvénytelen ismétlési szabály.");
        }
    }
}
