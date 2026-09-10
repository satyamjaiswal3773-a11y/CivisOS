using CivisOS.Domain.Common;

namespace CivisOS.Domain.Entities;

public class AttendanceBreak : BaseEntity
{
    public Guid EmployeeAttendanceDayId { get; set; }
    public EmployeeAttendanceDay EmployeeAttendanceDay { get; set; } = null!;

    public DateTime BreakStartUtc { get; set; }
    public DateTime BreakEndUtc { get; set; }
    public int DurationMinutes { get; set; }
}
