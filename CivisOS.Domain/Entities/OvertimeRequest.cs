using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class OvertimeRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly OvertimeDate { get; set; }
    public decimal RequestedHours { get; set; }
    public decimal? ApprovedHours { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }

    public AttendanceApprovalStatus Status { get; set; } = AttendanceApprovalStatus.Pending;
    public string RequestedByUserId { get; set; } = string.Empty;
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? RejectedByUserId { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? ApproverRemarks { get; set; }

    public bool IsPayable { get; set; }
}
