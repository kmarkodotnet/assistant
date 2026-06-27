namespace FamilyOs.Application.Notes.Common;

public interface IMarkdownSanitizer
{
    string Sanitize(string markdown);
}
