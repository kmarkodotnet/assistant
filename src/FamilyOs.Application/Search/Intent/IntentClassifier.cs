namespace FamilyOs.Application.Search.Intent;

public enum SearchIntent { Filter, Lookup, Find, Summarize }

public static class IntentClassifier
{
    public static (SearchIntent intent, double confidence) Classify(string query)
    {
        var q = query.ToLowerInvariant();

        if (ContainsAny(q, "összes", "minden", "mutasd", "lista", "listázd"))
            return (SearchIntent.Filter, 0.8);

        if (ContainsAny(q, "mikor", "hány", "melyik dátum", "lejár", "határidő"))
            return (SearchIntent.Lookup, 0.8);

        if (ContainsAny(q, "hol van", "hol találom", "keress", "találj"))
            return (SearchIntent.Find, 0.75);

        if (ContainsAny(q, "mit döntöttünk", "foglald össze", "összefoglalás", "röviden"))
            return (SearchIntent.Summarize, 0.75);

        return (SearchIntent.Find, 0.4); // low confidence — use hybrid
    }

    private static bool ContainsAny(string s, params string[] terms) => terms.Any(s.Contains);
}
