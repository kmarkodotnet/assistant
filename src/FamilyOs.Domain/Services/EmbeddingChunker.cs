namespace FamilyOs.Domain.Services;

public static class EmbeddingChunker
{
    private const int MaxChunkChars = 3200; // ~800 tokens x 4 chars/token
    private const int OverlapChars = 400;   // ~100 token overlap

    public static IReadOnlyList<string> Chunk(string text)
    {
        // Split by paragraph breaks (\n\n) first
        // Then merge small paragraphs / split large ones to stay near MaxChunkChars
        // Add overlap from previous chunk's tail
        var chunks = new List<string>();
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var current = new System.Text.StringBuilder();

        foreach (var para in paragraphs)
        {
            if (current.Length + para.Length > MaxChunkChars && current.Length > 0)
            {
                var chunkText = current.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(chunkText)) chunks.Add(chunkText);
                // overlap: take last OverlapChars of previous chunk
                var overlap = chunkText.Length > OverlapChars ? chunkText[^OverlapChars..] : chunkText;
                current.Clear();
                current.Append(overlap);
                current.Append("\n\n");
            }
            current.Append(para);
            current.Append("\n\n");
        }

        if (current.Length > 0)
        {
            var last = current.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(last)) chunks.Add(last);
        }

        return chunks.Count > 0 ? chunks : [text.Trim()];
    }
}
