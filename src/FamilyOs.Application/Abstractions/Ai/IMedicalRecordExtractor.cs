namespace FamilyOs.Application.Abstractions.Ai;

public record MedicalRecordExtraction(string? RecordType, string? DoctorName, DateOnly? RecordDate, string? Diagnosis, string? Notes);

public interface IMedicalRecordExtractor
{
    Task<MedicalRecordExtraction?> ExtractAsync(string text, CancellationToken ct = default);
}
