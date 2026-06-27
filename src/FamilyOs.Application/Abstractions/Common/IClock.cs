namespace FamilyOs.Application.Abstractions.Common;

public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
