using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class NoteTag
{
    public Guid NoteId { get; set; }
    public Guid TagId { get; set; }
    public Origin Origin { get; set; }
    public bool IsApproved { get; set; }

    public Note? Note { get; set; }
    public Tag? Tag { get; set; }
}
