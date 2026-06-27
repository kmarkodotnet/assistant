namespace FamilyOs.Domain.Entities;

public sealed class NoteTopic
{
    public Guid NoteId { get; set; }
    public Guid TopicId { get; set; }

    public Note? Note { get; set; }
    public Topic? Topic { get; set; }
}
