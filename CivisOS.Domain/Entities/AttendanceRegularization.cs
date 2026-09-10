using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class AttendanceRegularization : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly AttendanceDate { get; set; }
    public DateTime? RequestedInUtc { get; set; }
    public DateTime? RequestedOutUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? AttachmentReference { get; set; }

    public AttendanceApprovalStatus Status { get; set; } = AttendanceApprovalStatus.Pending;
    public string RequestedByUserId { get; set; } = string.Empty;

    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? RejectedByUserId { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? ApproverRemarks { get; set; }
}
