using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Services;

public sealed class DefaultReminderPolicy
{
    public static IReadOnlyList<(int OffsetDays, string Channel)> GetOffsets(DeadlineCategory category)
        => category switch
        {
            DeadlineCategory.Insurance => [(30, "InApp"), (7, "InApp"), (1, "InApp")],
            DeadlineCategory.Medical => [(7, "InApp"), (1, "InApp")],
            DeadlineCategory.Invoice => [(14, "InApp"), (3, "InApp")],
            DeadlineCategory.Inspection => [(30, "InApp"), (7, "InApp")],
            DeadlineCategory.School => [(7, "InApp"), (1, "InApp")],
            DeadlineCategory.Subscription => [(14, "InApp"), (3, "InApp")],
            _ => [(7, "InApp"), (1, "InApp")]
        };
}
