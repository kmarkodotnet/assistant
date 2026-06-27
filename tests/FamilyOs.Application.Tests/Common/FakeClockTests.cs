namespace FamilyOs.Application.Tests.Common;

public sealed class FakeClockTests
{
    [Fact]
    public void Advance_IncreasesUtcNow()
    {
        var clock = new FakeClock();
        var before = clock.UtcNow;

        clock.Advance(TimeSpan.FromHours(1));

        clock.UtcNow.Should().Be(before.AddHours(1));
    }

    [Fact]
    public void Today_ReflectsUtcNow()
    {
        var clock = new FakeClock { UtcNow = new DateTime(2026, 6, 26, 0, 0, 0, DateTimeKind.Utc) };

        clock.Today.Should().Be(new DateOnly(2026, 6, 26));
    }
}
