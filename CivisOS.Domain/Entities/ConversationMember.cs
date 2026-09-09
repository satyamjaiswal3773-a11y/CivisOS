using CivisOS.Domain.Common;

namespace CivisOS.Domain.Entities;

public class ConversationMember : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsAdmin { get; set; }
}
