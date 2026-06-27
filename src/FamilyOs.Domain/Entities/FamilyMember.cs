using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class FamilyMember
{
    private FamilyMember() { } // EF Core

    public Guid Id { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? FullName { get; private set; }
    public Relation Relation { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public bool HasUserAccount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    // Navigation
    public UserAccount? UserAccount { get; private set; }

    public static FamilyMember Create(
        string displayName,
        Relation relation,
        string? fullName = null,
        DateOnly? birthDate = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 100)
            throw new ArgumentException("DisplayName must be 1–100 characters.", nameof(displayName));
        if (birthDate.HasValue && birthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("BirthDate cannot be in the future.", nameof(birthDate));

        return new FamilyMember
        {
            Id = NewId(),
            DisplayName = displayName.Trim(),
            FullName = fullName?.Trim(),
            Relation = relation,
            BirthDate = birthDate,
            Notes = notes?.Trim(),
            HasUserAccount = false,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };
    }

    public void Update(
        string displayName,
        Relation relation,
        string? fullName,
        DateOnly? birthDate,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 100)
            throw new ArgumentException("DisplayName must be 1–100 characters.", nameof(displayName));

        DisplayName = displayName.Trim();
        Relation = relation;
        FullName = fullName?.Trim();
        BirthDate = birthDate;
        Notes = notes?.Trim();
    }

    public void MarkHasUserAccount(bool value) => HasUserAccount = value;

    public void SoftDelete() => DeletedUtc = DateTime.UtcNow;

    private static Guid NewId() => Guid.NewGuid(); // UUID v7 later via IUuidGenerator
}
