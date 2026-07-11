namespace FamilyOs.Application.Ai;

/// <summary>
/// Extracts the first balanced {...} block from raw LLM output (ai-pipeline.md §11.3 step 1) —
/// the model sometimes prepends explanatory text before the JSON object.
/// </summary>
public static class JsonBlockExtractor
{
    public static string? ExtractFirstObject(string content)
    {
        var start = content.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < content.Length; i++)
        {
            var c = content[i];

            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            switch (c)
            {
                case '"': inString = true; break;
                case '{': depth++; break;
                case '}':
                    depth--;
                    if (depth == 0)
                        return content[start..(i + 1)];
                    break;
            }
        }

        return null; // unbalanced — no complete object found
    }
}
