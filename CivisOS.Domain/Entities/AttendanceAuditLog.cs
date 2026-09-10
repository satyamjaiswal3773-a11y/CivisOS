using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class AttendanceAuditLog : BaseEntity
{
    public string? UserId { get; set; }
    public Guid? EmployeeId { get; set; }
    public DateOnly? AttendanceDate { get; set; }
    public AttendanceAuditAction Action { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
