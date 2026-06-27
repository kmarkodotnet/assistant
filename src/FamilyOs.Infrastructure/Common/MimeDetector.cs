using FamilyOs.Application.Abstractions.Common;

namespace FamilyOs.Infrastructure.Common;

public sealed class MimeDetector : IMimeDetector
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/heic",
        "text/plain",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    public string DetectMimeType(Stream stream)
    {
        var header = new byte[8];
        var read = stream.Read(header, 0, 8);
        if (stream.CanSeek) stream.Position = 0;

        // PDF: %PDF
        if (read >= 4 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
            return "application/pdf";

        // JPEG: FF D8 FF
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return "image/jpeg";

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            return "image/png";

        // DOCX (ZIP signature): 50 4B 03 04
        if (read >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04)
            return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        // HEIC: ftyp box at offset 4
        if (read >= 8 && header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70)
            return "image/heic";

        return "text/plain";
    }

    public bool IsAllowed(string mimeType) => Allowed.Contains(mimeType);
}
