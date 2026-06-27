namespace FamilyOs.Infrastructure.Ai.Prompts;

public static class PromptCatalog
{
    public const string SysPrefix = "sysprefix.v1.txt";
    public const string Summarize = "summarize.v1.txt";
    public const string Classify = "classify.v1.txt";
    public const string ExtractDeadlines = "extract-deadlines.v1.txt";
    public const string ExtractTasks = "extract-tasks.v1.txt";
    public const string ExtractWarranty = "extract-warranty.v1.txt";
    public const string ExtractMedical = "extract-medical.v1.txt";
    public const string ExtractFinancial = "extract-financial.v1.txt";

    public static string GetVersion(string resourceName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(resourceName);
        var parts = nameWithoutExt.Split('.');
        return parts.Length > 1 ? parts[^1] : "v1";
    }
}
