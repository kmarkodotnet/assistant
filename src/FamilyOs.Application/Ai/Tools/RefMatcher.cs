namespace FamilyOs.Application.Ai.Tools;

public enum RefMatchOutcome { NotFound, Ambiguous, Found }

/// <summary>
/// Shared name/title resolution helper for the 3 tools: exact case-insensitive match first,
/// falling back to substring match; ambiguous (&gt;1) or empty (0) never guesses
/// (ai-pipeline.md §11.2: "a resolve soha nem tippel").
/// </summary>
public static class RefMatcher
{
    public static (RefMatchOutcome Outcome, T? Match) Match<T>(
        IEnumerable<T> candidates, string reference, Func<T, string?> selector)
    {
        var list = candidates.ToList();

        var exact = list.Where(c => string.Equals(selector(c), reference, StringComparison.OrdinalIgnoreCase)).ToList();
        var pool = exact.Count > 0
            ? exact
            : list.Where(c => selector(c) is { } s && s.Contains(reference, StringComparison.OrdinalIgnoreCase)).ToList();

        return pool.Count switch
        {
            0 => (RefMatchOutcome.NotFound, default),
            1 => (RefMatchOutcome.Found, pool[0]),
            _ => (RefMatchOutcome.Ambiguous, default),
        };
    }
}
