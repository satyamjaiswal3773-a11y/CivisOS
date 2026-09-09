using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class Conversation : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public ConversationType Type { get; set; } = ConversationType.Private;
    public string? CreatedByUserId { get; set; }

    public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
