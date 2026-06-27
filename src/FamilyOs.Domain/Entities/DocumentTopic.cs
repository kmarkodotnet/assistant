using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class DocumentTopic
{
    public Guid DocumentId { get; set; }
    public Guid TopicId { get; set; }
    public Origin Origin { get; set; }
    public bool IsApproved { get; set; }
    public Document? Document { get; set; }
    public Topic? Topic { get; set; }
}
