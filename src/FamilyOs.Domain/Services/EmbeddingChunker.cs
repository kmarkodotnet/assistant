namespace FamilyOs.Domain.Services;

public static class EmbeddingChunker
{
    private const int MaxChunkChars = 3200; // ~800 tokens x 4 chars/token
    private const int OverlapChars = 400;   // ~100 token overlap

    public static IReadOnlyList<string> Chunk(string text)
    {
        var chunks = new List<string>();
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var current = new System.Text.StringBuilder();

        foreach (var para in paragraphs)
        {
            // If a single paragraph exceeds the limit, split it by sentence/word boundary
            var subParas = para.Length > MaxChunkChars ? SplitLarge(para) : [para];

            foreach (var sub in subParas)
            {
                if (current.Length + sub.Length > MaxChunkChars && current.Length > 0)
                {
                    var chunkText = current.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(chunkText)) chunks.Add(chunkText);
                    var overlap = chunkText.Length > OverlapChars ? chunkText[^OverlapChars..] : chunkText;
                    current.Clear();
                    current.Append(overlap);
                    current.Append("\n\n");
                }
                current.Append(sub);
                current.Append("\n\n");
            }
        }

        if (current.Length > 0)
        {
            var last = current.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(last)) chunks.Add(last);
        }

        return chunks.Count > 0 ? chunks : [text[..Math.Min(text.Length, MaxChunkChars)].Trim()];
    }

    private static IEnumerable<string> SplitLarge(string text)
    {
        var start = 0;
        while (start < text.Length)
        {
            var end = Math.Min(start + MaxChunkChars, text.Length);
            // Try to break at a space to avoid mid-word splits
            if (end < text.Length)
            {
                var spaceIdx = text.LastIndexOf(' ', end, Math.Min(end - start, 200));
                if (spaceIdx > start) end = spaceIdx;
            }
            yield return text[start..end].Trim();
            start = end;
        }
    }
}
