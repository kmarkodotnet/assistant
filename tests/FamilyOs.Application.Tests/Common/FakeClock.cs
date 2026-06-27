using FamilyOs.Application.Abstractions.Common;

namespace FamilyOs.Application.Tests.Common;

public sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new DateTime(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
    public DateOnly Today => DateOnly.FromDateTime(UtcNow);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
