namespace FamilyOs.Domain.Enums;

// Cancelled: explicit user-action; Skipped: automatic (lecsúszott/eszkalált)
public enum ReminderStatus { Scheduled, Fired, Acknowledged, Skipped, Failed, Cancelled }
