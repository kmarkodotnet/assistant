namespace FamilyOs.Application.Abstractions.Common;

public interface IMimeDetector
{
    string DetectMimeType(Stream stream);
    bool IsAllowed(string mimeType);
}
