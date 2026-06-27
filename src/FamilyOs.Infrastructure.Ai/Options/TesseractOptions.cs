namespace FamilyOs.Infrastructure.Ai.Options;

public sealed class TesseractOptions
{
    public const string Section = "Tesseract";
    public string DataPath { get; set; } = "/usr/share/tesseract-ocr/5/tessdata";
    public string Languages { get; set; } = "hun+eng";
}
