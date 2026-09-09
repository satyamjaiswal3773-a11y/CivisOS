using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class AiAlert : BaseEntity
{
    public AiAlertType AlertType { get; set; }
    public AiAlertSeverity Severity { get; set; } = AiAlertSeverity.Warning;
    public AiAlertStatus Status { get; set; } = AiAlertStatus.Open;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string? AcknowledgedByUserId { get; set; }
}
