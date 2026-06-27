namespace FamilyOs.Application.Search.Rrf;

public static class ReciprocalRankFusion
{
    public static IReadOnlyList<(Guid id, double score)> Fuse(
        IReadOnlyList<Guid> ftsRanked,
        IReadOnlyList<Guid> vectorRanked,
        int k = 60)
    {
        var scores = new Dictionary<Guid, double>();

        for (int i = 0; i < ftsRanked.Count; i++)
            scores[ftsRanked[i]] = scores.GetValueOrDefault(ftsRanked[i]) + 1.0 / (k + i + 1);

        for (int i = 0; i < vectorRanked.Count; i++)
            scores[vectorRanked[i]] = scores.GetValueOrDefault(vectorRanked[i]) + 1.0 / (k + i + 1);

        return scores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }
}
