namespace FamilyOs.Application.Common.Errors;

public sealed class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(
            "Validation failed.",
            "Érvénytelen kérés. Kérjük, ellenőrizze az adatokat.")
    {
        Errors = errors;
    }
}
