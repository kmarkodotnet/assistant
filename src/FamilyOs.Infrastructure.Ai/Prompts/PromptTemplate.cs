namespace FamilyOs.Infrastructure.Ai.Prompts;

public static class PromptTemplate
{
    public static string Load(string resourceName)
    {
        var assembly = typeof(PromptTemplate).Assembly;
        var fullName = $"FamilyOs.Infrastructure.Ai.Prompts.{resourceName}";
        using var stream = assembly.GetManifestResourceStream(fullName)
            ?? throw new InvalidOperationException($"Embedded resource '{fullName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string Replace(string template, IDictionary<string, string> placeholders)
    {
        foreach (var (key, value) in placeholders)
            template = template.Replace($"{{{{{key}}}}}", value);
        return template;
    }
}
