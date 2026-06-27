using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class DocumentTag
{
    public Guid DocumentId { get; set; }
    public Guid TagId { get; set; }
    public Origin Origin { get; set; }
    public bool IsApproved { get; set; }
    public Document? Document { get; set; }
    public Tag? Tag { get; set; }
}
