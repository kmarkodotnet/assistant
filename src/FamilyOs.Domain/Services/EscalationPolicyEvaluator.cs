namespace FamilyOs.Domain.Services;

public static class EscalationPolicyEvaluator
{
    public const int MaxEscalationLevel = 3;

    public static TimeSpan GetEscalationTimeout(int currentLevel) => currentLevel switch
    {
        0 => TimeSpan.FromHours(24),
        1 => TimeSpan.FromHours(48),
        _ => TimeSpan.FromHours(72),
    };

    public static bool CanEscalate(int currentLevel) => currentLevel < MaxEscalationLevel;
}
