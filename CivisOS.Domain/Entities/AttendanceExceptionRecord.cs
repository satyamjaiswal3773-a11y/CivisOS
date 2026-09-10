using CivisOS.Domain.Common;
using CivisOS.Domain.Enums;

namespace CivisOS.Domain.Entities;

public class AttendanceExceptionRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly AttendanceDate { get; set; }
    public AttendanceExceptionType ExceptionType { get; set; }
    public AttendanceExceptionStatus Status { get; set; } = AttendanceExceptionStatus.Open;
    public string Message { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }

    public Guid? EmployeeAttendanceDayId { get; set; }
    public EmployeeAttendanceDay? EmployeeAttendanceDay { get; set; }
}
