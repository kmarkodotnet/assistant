using FamilyOs.Application.Notes.Common;
using System.Text.RegularExpressions;

namespace FamilyOs.Infrastructure.Markdown;

public sealed class MarkdownSanitizer : IMarkdownSanitizer
{
    private static readonly Regex ScriptTagRegex = new(
        @"<script[^>]*>.*?</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex OnAttributeRegex = new(
        @"\s*on\w+\s*=\s*""[^""]*""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JavascriptUrlRegex = new(
        @"href\s*=\s*""javascript:[^""]*""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Sanitize(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var pipeline = new Markdig.MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var html = Markdig.Markdown.ToHtml(markdown, pipeline);

        html = ScriptTagRegex.Replace(html, string.Empty);
        html = OnAttributeRegex.Replace(html, string.Empty);
        html = JavascriptUrlRegex.Replace(html, "href=\"#\"");

        return html;
    }

    public static string SanitizeStatic(string markdown)
    {
        var instance = new MarkdownSanitizer();
        return instance.Sanitize(markdown);
    }
}
